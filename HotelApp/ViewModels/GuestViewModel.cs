using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using HotelApp.Models;
using HotelApp.Services;

namespace HotelApp.ViewModels
{
	public class GuestsViewModel : INotifyPropertyChanged
	{
		// Порог VIP-статуса: больше 100 000 ₽ или больше 5 суток
		private const double VipRevenueThreshold = 100000;
		private const int VipDaysThreshold = 5;

		private IntPtr _hotelPtr;
		private MainViewModel _mainVm;
		private string _searchText = string.Empty;

		public ObservableCollection<GuestModel> Guests { get; } = new ObservableCollection<GuestModel>();

		public string SearchText
		{
			get => _searchText;
			set
			{
				if (_searchText == value) return;
				_searchText = value;
				OnPropertyChanged();
				RefreshData();
			}
		}

		public GuestsViewModel(IntPtr hotelPtr, MainViewModel mainVm)
		{
			_hotelPtr = hotelPtr;
			_mainVm = mainVm;
			RefreshData();
		}

		// Снимок всех гостей с агрегатами по проживаниям и броням — для отчётов.
		// Без записи в журнал и модальных окон: исключение обрабатывает вызывающий код.
		public static List<GuestModel> LoadSnapshot(IntPtr hotelPtr)
		{
			var vm = new GuestsViewModel(hotelPtr, null, false);
			vm.LoadGuests();
			return vm.Guests.ToList();
		}

		private GuestsViewModel(IntPtr hotelPtr, MainViewModel mainVm, bool load)
		{
			_hotelPtr = hotelPtr;
			_mainVm = mainVm;
			if (load) RefreshData();
		}

		public void RefreshData()
		{
			try { LoadGuests(); }
			catch (Exception ex)
			{
				if (_mainVm != null)
					_mainVm.AddLog("База гостей: не удалось загрузить данные. " + ex.Message, LogType.Error);
				AppDialog.Error("Не удалось загрузить список гостей.\n" + ex.Message, "База гостей");
			}
		}

		private void LoadGuests()
		{
			const int max = 1000;
			var buffer = new CGuest[max];
			int count = NativeMethods.Hotel_GetAllGuests(_hotelPtr, buffer, max);

			// 1. Справочник номеров: roomId -> (buildingName, roomNumber, basePrice, typeText)
			var roomInfo = new Dictionary<int, (string Building, string Number, double Price, string TypeText)>();
			var rooms = new CRoom[max];
			int roomCount = NativeMethods.Hotel_GetAllRooms(_hotelPtr, rooms, max);
			var buildings = new Dictionary<int, string>();
			var bBuffer = new CBuilding[100];
			int bCount = NativeMethods.Hotel_GetAllBuildings(_hotelPtr, bBuffer, 100);
			for (int i = 0; i < bCount; i++) buildings[bBuffer[i].Id] = bBuffer[i].Name;

			for (int i = 0; i < roomCount; i++)
			{
				string bName = buildings.TryGetValue(rooms[i].BuildingId, out string n) ? n : $"Корпус {rooms[i].BuildingId}";
				string typeText = rooms[i].Type == 1 ? "Люкс" : (rooms[i].Type == 2 ? "Конференц-зал" : "Стандарт");
				roomInfo[rooms[i].Id] = (bName, rooms[i].Number, rooms[i].BasePrice, typeText);
			}

			// 2. Собираем историю по каждому гостю (проживания + брони)
			var historyByGuest = BuildHistory(roomInfo);

			// 3. Фильтр и формирование GuestModel
			string search = (SearchText ?? string.Empty).Trim().ToLower();
			Guests.Clear();
			int rowNumber = 0;

			for (int i = 0; i < count; i++)
			{
				if (search.Length > 0)
				{
					string fullName = (buffer[i].FullName ?? string.Empty).ToLower();
					string passport = (buffer[i].Passport ?? string.Empty).ToLower();
					if (!fullName.Contains(search) && !passport.Contains(search)) continue;
				}
				rowNumber++;

				string key = StayKey(buffer[i].FullName, buffer[i].Passport) ?? string.Empty;
				var history = historyByGuest.TryGetValue(key, out var list)
					? list
					: new List<StayHistoryItem>();

				// Сортируем по дате заезда
				history = history.OrderBy(h => h.CheckIn).ToList();

				var guest = new GuestModel
				{
					Id = buffer[i].Id,
					RowNumber = rowNumber,
					FullName = buffer[i].FullName,
					Passport = buffer[i].Passport
				};

				// Агрегация
				int totalDays = 0;
				double totalRevenue = 0;
				foreach (var h in history)
				{
					totalDays += h.Days;
					totalRevenue += h.Revenue;
					guest.StayHistory.Add(h);
				}

				guest.TotalDays = totalDays;
				guest.TotalRevenue = totalRevenue;
				guest.LastRoomInfo = ComputeLastRoomInfo(history);

				// VIP: больше 100 000 ₽ или больше 5 суток проживания
				guest.IsVip = totalRevenue > VipRevenueThreshold || totalDays > VipDaysThreshold;

				// Старые свойства (для совместимости, если где-то используются)
				var nearest = PickNearest(history);
				guest.StayDisplay = nearest != null
					? $"{nearest.CheckIn:dd.MM.yyyy} — {nearest.CheckOut:dd.MM.yyyy}"
					: "—";

				Guests.Add(guest);
			}
		}

		// Собирает историю проживаний и броней по ключу гостя
		private Dictionary<string, List<StayHistoryItem>> BuildHistory(
			Dictionary<int, (string Building, string Number, double Price, string TypeText)> roomInfo)
		{
			var result = new Dictionary<string, List<StayHistoryItem>>(StringComparer.OrdinalIgnoreCase);
			var today = DateTime.Today;
			var todayStr = today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

			// 1. Текущие проживания (статус = 1)
			var rooms = new CRoom[1000];
			int roomCount = NativeMethods.Hotel_GetAllRooms(_hotelPtr, rooms, 1000);
			for (int i = 0; i < roomCount; i++)
			{
				if (rooms[i].Status != 1) continue;
				var key = StayKey(rooms[i].GuestName, rooms[i].GuestPassport);
				if (string.IsNullOrEmpty(key)) continue;
				if (!roomInfo.TryGetValue(rooms[i].Id, out var info)) continue;

				if (!DateTime.TryParse(rooms[i].CheckInDate, out var cin)) continue;
				if (!DateTime.TryParse(rooms[i].CheckOutDate, out var cout)) continue;
				int days = (cout.Date - cin.Date).Days;
				if (days <= 0) days = 1;

				AddHistory(result, key, new StayHistoryItem
				{
					RoomId = rooms[i].Id,
					CheckIn = cin,
					CheckOut = cout,
					Days = days,
					BuildingName = info.Building,
					RoomNumber = info.Number,
					RoomTypeText = info.TypeText,
					Revenue = info.Price * days,
					Status = StayStatus.Current
				});
			}

			// 2. Бронирования (все, включая будущие)
			var bookings = new CBooking[1000];
			int bookingCount = NativeMethods.Hotel_GetAllBookings(_hotelPtr, bookings, 1000);
			for (int i = 0; i < bookingCount; i++)
			{
				var key = StayKey(bookings[i].GuestName, bookings[i].Passport);
				if (string.IsNullOrEmpty(key)) continue;
				if (!roomInfo.TryGetValue(bookings[i].RoomId, out var info)) continue;

				if (!DateTime.TryParse(bookings[i].CheckInDate, out var cin)) continue;
				if (!DateTime.TryParse(bookings[i].CheckOutDate, out var cout)) continue;
				int days = (cout.Date - cin.Date).Days;
				if (days <= 0) days = 1;

				// Определяем статус
				StayStatus status;
				if (cin.Date <= today && cout.Date > today)
					status = StayStatus.Current; // бронь, которая уже идёт (гость заселен)
				else if (cin.Date > today)
					status = StayStatus.Booking; // будущая
				else
					status = StayStatus.Past;    // прошедшая (редко, но бывает)

				AddHistory(result, key, new StayHistoryItem
				{
					RoomId = bookings[i].RoomId,
					CheckIn = cin,
					CheckOut = cout,
					Days = days,
					BuildingName = info.Building,
					RoomNumber = info.Number,
					RoomTypeText = info.TypeText,
					Revenue = info.Price * days,
					Status = status
				});
			}

			return result;
		}

		// Добавляет запись, защищая от дубликатов по (roomId, checkIn, checkOut)
		private static void AddHistory(
			Dictionary<string, List<StayHistoryItem>> dict,
			string key, StayHistoryItem item)
		{
			if (!dict.TryGetValue(key, out var list))
			{
				list = new List<StayHistoryItem>();
				dict[key] = list;
			}
			// Дубликат: тот же номер и те же даты
			if (list.Any(h => h.RoomId == item.RoomId
				&& h.CheckIn.Date == item.CheckIn.Date
				&& h.CheckOut.Date == item.CheckOut.Date))
				return;
			list.Add(item);
		}

		// Вычисляет LastRoomInfo по правилу: текущий > ближайшая бронь > последний прошлый > "—"
		private static string ComputeLastRoomInfo(List<StayHistoryItem> history)
		{
			if (history == null || history.Count == 0) return "—";

			// Приоритет 1: текущее проживание
			var current = history.FirstOrDefault(h => h.Status == StayStatus.Current);
			if (current != null)
				return $"{current.BuildingName}, {current.RoomNumber}";

			// Приоритет 2: ближайшая будущая бронь
			var booking = history
				.Where(h => h.Status == StayStatus.Booking)
				.OrderBy(h => h.CheckIn)
				.FirstOrDefault();
			if (booking != null)
				return $"{booking.BuildingName}, {booking.RoomNumber} (бронь)";

			// Приоритет 3: последний завершённый
			var past = history
				.Where(h => h.Status == StayStatus.Past)
				.OrderByDescending(h => h.CheckOut)
				.FirstOrDefault();
			if (past != null)
				return $"{past.BuildingName}, {past.RoomNumber}";

			return "—";
		}

		// Ближайшее проживание для старого StayDisplay
		private static StayHistoryItem PickNearest(List<StayHistoryItem> list)
		{
			if (list == null || list.Count == 0) return null;
			var today = DateTime.Today;
			var ordered = list.OrderBy(h => h.CheckIn).ToList();
			foreach (var h in ordered)
				if (h.CheckIn.Date <= today && h.CheckOut.Date > today) return h;
			foreach (var h in ordered)
				if (h.CheckIn.Date > today) return h;
			return ordered[ordered.Count - 1];
		}

		// Удаление гостя (без изменений)
		public bool DeleteGuest(GuestModel guest)
		{
			if (guest == null) return false;
			try
			{
				string roomNumber = FindStayingRoomNumber(guest);
				if (roomNumber != null)
				{
					_mainVm.AddLog($"Удаление гостя {guest.FullName} отменено: гость проживает в номере {roomNumber}.", LogType.Error);
					AppDialog.Warning($"Гость {guest.FullName} проживает в номере {roomNumber}.\nСначала выселите гостя.",
						"Удаление гостя");
					return false;
				}
				if (!NativeMethods.Hotel_DeleteGuest(_hotelPtr, guest.Id))
				{
					_mainVm.AddLog($"Удаление гостя {guest.FullName} не выполнено.", LogType.Error);
					AppDialog.Warning("Не удалось удалить гостя.", "Удаление гостя");
					return false;
				}
				RefreshData();
				_mainVm.AddLog($"Удалён гость: {guest.FullName} (паспорт: {guest.Passport}).", LogType.Success);
				_mainVm.SaveData();
				return true;
			}
			catch (Exception ex)
			{
				_mainVm.AddLog("Удаление гостя: " + ex.Message, LogType.Error);
				AppDialog.Error("Не удалось удалить гостя.\n" + ex.Message, "Удаление гостя");
				return false;
			}
		}

		public string FindStayingRoomNumber(GuestModel guest)
		{
			if (guest == null) return null;
			string key = StayKey(guest.FullName, guest.Passport);
			if (key == null) return null;
			var rooms = new CRoom[1000];
			int count = NativeMethods.Hotel_GetAllRooms(_hotelPtr, rooms, 1000);
			for (int i = 0; i < count; i++)
			{
				if (rooms[i].Status != 1) continue;
				if (!string.Equals(StayKey(rooms[i].GuestName, rooms[i].GuestPassport), key,
						StringComparison.OrdinalIgnoreCase)) continue;
				return rooms[i].Number;
			}
			return null;
		}

		private static string StayKey(string name, string passport)
		{
			if (!string.IsNullOrWhiteSpace(passport)) return "P:" + passport.Trim().ToUpperInvariant();
			if (!string.IsNullOrWhiteSpace(name)) return "N:" + name.Trim().ToUpperInvariant();
			return null;
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string name = null)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}
}