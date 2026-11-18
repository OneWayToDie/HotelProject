using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using HotelApp.Models;
using HotelApp.Services;
using static HotelApp.Services.NativeMethods;

namespace HotelApp.ViewModels
{
	public class RoomsViewModel : INotifyPropertyChanged
	{
		private IntPtr _hotelPtr;
		private MainViewModel _mainVm; // Ссылка на главную VM
		private string _searchText = string.Empty;

		public ObservableCollection<BuildingModel> Buildings { get; } = new ObservableCollection<BuildingModel>();
		public ObservableCollection<RoomModel> Rooms { get; } = new ObservableCollection<RoomModel>();
		public ObservableCollection<BookingModel> Bookings { get; } = new ObservableCollection<BookingModel>();

		// Текущий фильтр: 0 - Все, 1 - Свободные, 2 - Занятые
		public int CurrentFilter { get; set; } = 0;

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

		public RoomsViewModel(IntPtr hotelPtr, MainViewModel mainVm)
		{
			_hotelPtr = hotelPtr;
			_mainVm = mainVm;
			RefreshData();
		}

		public void RefreshData()
		{
			try
			{
				LoadRooms();
			}
			catch (Exception ex)
			{
				_mainVm.AddLog($"Номера и Фонд: не удалось загрузить данные. {ex.Message}", LogType.Error);
				AppDialog.Error($"Не удалось загрузить список номеров.\n{ex.Message}", "Номера и Фонд");
			}
		}

		private void LoadRooms()
		{
			// Загружаем корпуса
			var bBuffer = new CBuilding[100];
			int bCount = NativeMethods.Hotel_GetAllBuildings(_hotelPtr, bBuffer, 100);
			Buildings.Clear();
			for (int i = 0; i < bCount; i++)
				Buildings.Add(new BuildingModel { Id = bBuffer[i].Id, Name = bBuffer[i].Name });

			// Загружаем номера
			var rBuffer = new CRoom[1000];
			int rCount = NativeMethods.Hotel_GetAllRooms(_hotelPtr, rBuffer, 1000);

			string search = (SearchText ?? string.Empty).Trim().ToLower();

			Rooms.Clear();
			for (int i = 0; i < rCount; i++)
			{
				// ПРИМЕНЯЕМ ФИЛЬТР
				if (CurrentFilter == 1 && rBuffer[i].Status != 0) continue; // Показываем только Свободные
				if (CurrentFilter == 2 && rBuffer[i].Status != 1) continue; // Показываем только Занятые

				// ПРИМЕНЯЕМ ПОИСК (по номеру и по ФИО гостя, без учета регистра)
				if (search.Length > 0)
				{
					string number = (rBuffer[i].Number ?? string.Empty).ToLower();
					string guest = (rBuffer[i].GuestName ?? string.Empty).ToLower();

					if (!number.Contains(search) && !guest.Contains(search)) continue;
				}

				Rooms.Add(new RoomModel
				{
					Id = rBuffer[i].Id,
					BuildingId = rBuffer[i].BuildingId,
					Number = rBuffer[i].Number,
					Capacity = rBuffer[i].Capacity,
					BasePrice = rBuffer[i].BasePrice,
					Status = rBuffer[i].Status,
					Type = rBuffer[i].Type,
					GuestName = rBuffer[i].GuestName
				});
			}
			_mainVm.UpdateStats(); // Обновляем цифры в шапке

			// Загружаем бронирования
			var bkBuffer = new CBooking[1000];
			int bkCount = NativeMethods.Hotel_GetAllBookings(_hotelPtr, bkBuffer, 1000);

			Bookings.Clear();
			for (int i = 0; i < bkCount; i++)
			{
				Bookings.Add(new BookingModel
				{
					Id = bkBuffer[i].Id,
					RoomId = bkBuffer[i].RoomId,
					GuestName = bkBuffer[i].GuestName,
					Passport = bkBuffer[i].Passport,
					CheckInDate = bkBuffer[i].CheckInDate,
					CheckOutDate = bkBuffer[i].CheckOutDate
				});
			}

			// Показываем ближайшую бронь в таблице номеров
			foreach (var room in Rooms)
			{
				var booking = Bookings.Where(b => b.RoomId == room.Id)
									  .OrderBy(b => b.CheckInDate)
									  .FirstOrDefault();
				room.BookingInfo = booking != null ? booking.DateDisplay : "—";
			}
		}


		public void CheckOut(int roomId)
		{
			var room = Rooms.FirstOrDefault(r => r.Id == roomId);
			string guestName = room?.GuestName;
			string roomNumber = room?.Number ?? roomId.ToString();

			try
			{
				double price = NativeMethods.Hotel_CheckOut(_hotelPtr, roomId);
				if (price > 0)
				{
					RefreshData();
					_mainVm.SaveData();
					_mainVm.AddLog($"Выселение: {guestName} из номера {roomNumber}. Оплата: {price:C}", LogType.Success);
					AppDialog.Success($"Гость выселен.\nК оплате: {price:C}", "Выселение");
				}
				else
				{
					_mainVm.AddLog($"Выселение из номера {roomNumber} не выполнено: в номере нет гостя.", LogType.Error);
					AppDialog.Warning("Не удалось выселить. В номере нет гостя.", "Ошибка");
				}
			}
			catch (Exception ex)
			{
				HandleError("выселение гостя", ex);
			}
		}

		// Завершить уборку
		public void FinishCleaning(int roomId)
		{
			var room = Rooms.FirstOrDefault(r => r.Id == roomId);
			string roomNumber = room?.Number ?? roomId.ToString();

			try
			{
				NativeMethods.Hotel_FinishCleaning(_hotelPtr, roomId);
				RefreshData();
				_mainVm.SaveData();
				_mainVm.AddLog($"Уборка: номер {roomNumber} снова свободен.", LogType.Success);
				AppDialog.Success("Уборка завершена! Номер снова свободен.", "Успех");
			}
			catch (Exception ex)
			{
				HandleError("завершение уборки", ex);
			}
		}

		public void UpdatePrice(int roomId, double newPrice)
		{
			var room = Rooms.FirstOrDefault(r => r.Id == roomId);
			string roomNumber = room?.Number ?? roomId.ToString();

			if (newPrice <= 0)
			{
				_mainVm.AddLog($"Номер {roomNumber}: цена не изменена (некорректное значение {newPrice}).", LogType.Error);
				AppDialog.Warning("Цена должна быть целым числом больше нуля!", "Ошибка");
				return;
			}

			try
			{
				NativeMethods.Hotel_UpdatePrice(_hotelPtr, roomId, newPrice);
				RefreshData();
				_mainVm.SaveData();
				_mainVm.AddLog($"Номер {roomNumber}: новая цена {newPrice:N0} ₽.", LogType.Success);
			}
			catch (Exception ex)
			{
				HandleError("изменение цены", ex);
			}
		}

		public void CheckIn(int roomId, string guestName, string passport, string inDate, string outDate, int days)
		{
			var room = Rooms.FirstOrDefault(r => r.Id == roomId);
			string roomNumber = room?.Number ?? roomId.ToString();

			try
			{
				// Нельзя заселить на даты, которые уже забронированы другим гостем
				if (_mainVm.HasBookingConflict(roomId, inDate, outDate, guestName, passport))
				{
					_mainVm.AddLog($"Заселение {guestName} в номер {roomNumber} не выполнено: даты заняты бронью.", LogType.Error);
					AppDialog.Warning($"Номер {roomNumber} забронирован на выбранные даты!\nВыберите другие даты или снимите бронь.",
						"Бронирование");
					return;
				}

				// Вызываем C++ с 6 аргументами
				bool success = NativeMethods.Hotel_CheckIn(_hotelPtr, roomId, guestName, passport, inDate, outDate, days);

				if (success)
				{
					RefreshData();
					_mainVm.SaveData(); // Сохраняем в файл
					_mainVm.AddLog($"Заселение: {guestName} -> номер {roomNumber}, {inDate} - {outDate} ({days} дн.).", LogType.Success);
					AppDialog.Success($"Гость {guestName} успешно заселен!", "Успех");
				}
				else
				{
					_mainVm.AddLog($"Заселение {guestName} в номер {roomNumber} не выполнено: номер занят или на уборке.", LogType.Error);
					AppDialog.Warning("Не удалось заселить. Возможно, номер занят или на уборке.", "Ошибка");
				}
			}
			catch (Exception ex)
			{
				HandleError("заселение гостя", ex);
			}
		}

		// Бронирование номера на будущие даты
		public void BookRoom(int roomId, string guestName, string passport, string inDate, string outDate)
		{
			var room = Rooms.FirstOrDefault(r => r.Id == roomId);
			string roomNumber = room?.Number ?? roomId.ToString();

			try
			{
				bool success = NativeMethods.Hotel_BookRoom(_hotelPtr, roomId, guestName, passport, inDate, outDate);

				if (success)
				{
					RefreshData();
					_mainVm.SaveData();
					_mainVm.AddLog($"Бронирование: {guestName} -> номер {roomNumber}, {inDate} - {outDate}.", LogType.Success);
					AppDialog.Success($"Номер {roomNumber} забронирован на {guestName}\n{inDate} - {outDate}",
						"Бронирование");
				}
				else
				{
					_mainVm.AddLog($"Бронирование {guestName} в номер {roomNumber} не выполнено: даты пересекаются с другой бронью.", LogType.Error);
					AppDialog.Warning("Не удалось забронировать: на выбранные даты номер уже забронирован или даты некорректны.",
						"Ошибка");
				}
			}
			catch (Exception ex)
			{
				HandleError("бронирование номера", ex);
			}
		}

		// Единая обработка ошибок нативного слоя: журнал + сообщение пользователю
		private void HandleError(string action, Exception ex)
		{
			_mainVm.AddLog($"{action}: {ex.Message}", LogType.Error);
			AppDialog.Error($"Не удалось выполнить действие «{action}».\n{ex.Message}", "Ошибка");
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string name = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}
	}
}
