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
		private IntPtr _hotelPtr;
		private MainViewModel _mainVm;
		private string _searchText = string.Empty;

		public ObservableCollection<GuestModel> Guests { get; } = new ObservableCollection<GuestModel>();

		// Строка живого поиска (без кнопки "Найти")
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

		public void RefreshData()
		{
			try
			{
				LoadGuests();
			}
			catch (Exception ex)
			{
				_mainVm.AddLog("База гостей: не удалось загрузить данные. " + ex.Message, LogType.Error);
				AppDialog.Error("Не удалось загрузить список гостей.\n" + ex.Message, "База гостей");
			}
		}

		private void LoadGuests()
		{
			const int max = 1000;
			var buffer = new CGuest[max];
			int count = NativeMethods.Hotel_GetAllGuests(_hotelPtr, buffer, max);

			// Проживание и брони показываем отдельными колонками
			var stays = BuildStays(onlyBookings: false);
			var bookings = BuildStays(onlyBookings: true);

			string search = (SearchText ?? string.Empty).Trim().ToLower();

			Guests.Clear();
			int rowNumber = 0;
			for (int i = 0; i < count; i++)
			{
				// ПРИМЕНЯЕМ ПОИСК (по ФИО и паспорту, без учета регистра)
				if (search.Length > 0)
				{
					string fullName = (buffer[i].FullName ?? string.Empty).ToLower();
					string passport = (buffer[i].Passport ?? string.Empty).ToLower();

					if (!fullName.Contains(search) && !passport.Contains(search)) continue;
				}

				rowNumber++;

				string key = StayKey(buffer[i].FullName, buffer[i].Passport);
				stays.TryGetValue(key ?? string.Empty, out List<StayRange> guestStays);
				bookings.TryGetValue(key ?? string.Empty, out List<StayRange> guestBookings);

				Guests.Add(new GuestModel
				{
					Id = buffer[i].Id,
					RowNumber = rowNumber,
					FullName = buffer[i].FullName,
					Passport = buffer[i].Passport,
					StayDisplay = FormatNearest(guestStays),
					StayToolTip = BuildStayToolTip(guestStays, "Все проживания:"),
					BookingDisplay = FormatNearest(guestBookings),
					BookingToolTip = BuildStayToolTip(guestBookings, "Все брони:")
				});
			}
		}

		// Удаление гостя из базы. Id не смещаются: запись просто исчезает,
		// а номера строк в таблице пересчитываются в RefreshData.
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

		// Номер, в котором сейчас проживает гость (null — не проживает)
		public string FindStayingRoomNumber(GuestModel guest)
		{
			if (guest == null) return null;

			string key = StayKey(guest.FullName, guest.Passport);
			if (key == null) return null;

			var rooms = new CRoom[1000];
			int count = NativeMethods.Hotel_GetAllRooms(_hotelPtr, rooms, 1000);
			for (int i = 0; i < count; i++)
			{
				if (rooms[i].Status != 1) continue; // 1 = занят
				if (!string.Equals(StayKey(rooms[i].GuestName, rooms[i].GuestPassport), key,
						StringComparison.OrdinalIgnoreCase)) continue;
				return rooms[i].Number;
			}
			return null;
		}

		private Dictionary<string, List<StayRange>> BuildStays(bool onlyBookings)
		{
			var stays = new Dictionary<string, List<StayRange>>(StringComparer.OrdinalIgnoreCase);

			if (!onlyBookings)
			{
				var rooms = new CRoom[1000];
				int roomCount = NativeMethods.Hotel_GetAllRooms(_hotelPtr, rooms, 1000);
				for (int i = 0; i < roomCount; i++)
				{
					if (rooms[i].Status != 1) continue;
					AddStay(stays, StayKey(rooms[i].GuestName, rooms[i].GuestPassport),
						rooms[i].CheckInDate, rooms[i].CheckOutDate);
				}
			}

			var bookings = new CBooking[1000];
			int bookingCount = NativeMethods.Hotel_GetAllBookings(_hotelPtr, bookings, 1000);
			for (int i = 0; i < bookingCount; i++)
			{
				AddStay(stays, StayKey(bookings[i].GuestName, bookings[i].Passport),
					bookings[i].CheckInDate, bookings[i].CheckOutDate);
			}

			return stays;
		}

		private static string FormatNearest(List<StayRange> list)
		{
			var nearest = PickNearest(list);
			return nearest == null
				? "—"
				: $"{FormatDate(nearest.CheckIn)} — {FormatDate(nearest.CheckOut)}";
		}

		private static void AddStay(Dictionary<string, List<StayRange>> stays, string key,
			string checkIn, string checkOut)
		{
			if (string.IsNullOrEmpty(key) ||
				string.IsNullOrWhiteSpace(checkIn) || string.IsNullOrWhiteSpace(checkOut)) return;

			if (!stays.TryGetValue(key, out List<StayRange> list))
			{
				list = new List<StayRange>();
				stays[key] = list;
			}

			// Одна и та же поездка может быть и в номере, и в брони
			if (list.Any(s => s.CheckIn == checkIn && s.CheckOut == checkOut)) return;

			list.Add(new StayRange { CheckIn = checkIn, CheckOut = checkOut });
		}

		// Ключ сопоставления гостя с заселением/бронью: паспорт, а при его отсутствии — ФИО
		private static string StayKey(string name, string passport)
		{
			if (!string.IsNullOrWhiteSpace(passport)) return "P:" + passport.Trim().ToUpperInvariant();
			if (!string.IsNullOrWhiteSpace(name)) return "N:" + name.Trim().ToUpperInvariant();
			return null;
		}

		// Самое близкое проживание: текущее, затем ближайшее будущее, иначе последнее прошедшее
		private static StayRange PickNearest(List<StayRange> list)
		{
			if (list == null || list.Count == 0) return null;

			string today = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
			var ordered = list.OrderBy(s => s.CheckIn, StringComparer.Ordinal).ToList();

			foreach (var stay in ordered)
				if (string.CompareOrdinal(stay.CheckIn, today) <= 0 &&
					string.CompareOrdinal(today, stay.CheckOut) < 0)
					return stay;

			foreach (var stay in ordered)
				if (string.CompareOrdinal(stay.CheckIn, today) > 0)
					return stay;

			return ordered[ordered.Count - 1];
		}

		private static string BuildStayToolTip(List<StayRange> list, string header)
		{
			if (list == null || list.Count < 2) return null;

			var ordered = list.OrderBy(s => s.CheckIn, StringComparer.Ordinal);
			return header + "\n" + string.Join("\n",
				ordered.Select(s => $"{FormatDate(s.CheckIn)} — {FormatDate(s.CheckOut)}"));
		}

		private static string FormatDate(string iso)
		{
			if (DateTime.TryParseExact(iso, "yyyy-MM-dd", CultureInfo.InvariantCulture,
					DateTimeStyles.None, out DateTime date))
				return date.ToString("dd.MM.yyyy");
			return iso;
		}

		private sealed class StayRange
		{
			public string CheckIn;
			public string CheckOut;
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string name = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}
	}
}
