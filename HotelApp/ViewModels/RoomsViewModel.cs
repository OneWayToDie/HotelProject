using System;
using System.Collections.ObjectModel;
using System.Windows;
using HotelApp.Models;
using HotelApp.Services;
using static HotelApp.Services.NativeMethods;

namespace HotelApp.ViewModels
{
	public class RoomsViewModel
	{
		private IntPtr _hotelPtr;
		private MainViewModel _mainVm; // Ссылка на главную VM

		public ObservableCollection<BuildingModel> Buildings { get; } = new ObservableCollection<BuildingModel>();
		public ObservableCollection<RoomModel> Rooms { get; } = new ObservableCollection<RoomModel>();

		// Текущий фильтр: 0 - Все, 1 - Свободные, 2 - Занятые
		public int CurrentFilter { get; set; } = 0;

		public RoomsViewModel(IntPtr hotelPtr, MainViewModel mainVm)
		{
			_hotelPtr = hotelPtr;
			_mainVm = mainVm;
			RefreshData();
		}

		public void RefreshData()
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

			Rooms.Clear();
			for (int i = 0; i < rCount; i++)
			{
				// ПРИМЕНЯЕМ ФИЛЬТР
				if (CurrentFilter == 1 && rBuffer[i].Status != 0) continue; // Показываем только Свободные
				if (CurrentFilter == 2 && rBuffer[i].Status != 1) continue; // Показываем только Занятые

				Rooms.Add(new RoomModel
				{
					Id = rBuffer[i].Id,
					Number = rBuffer[i].Number,
					Capacity = rBuffer[i].Capacity,
					BasePrice = rBuffer[i].BasePrice,
					Status = rBuffer[i].Status,
					Type = rBuffer[i].Type,
					GuestName = rBuffer[i].GuestName
				});
			}
			_mainVm.UpdateStats(); // Обновляем цифры в шапке
		}


		public void CheckOut(int roomId)
		{
			double price = NativeMethods.Hotel_CheckOut(_hotelPtr, roomId);
			if (price > 0)
			{
				RefreshData();
				_mainVm.SaveData();
				MessageBox.Show($"Гость выселен.\nК оплате: {price:C}", "Выселение", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			else
			{
				MessageBox.Show("Не удалось выселить. В номере нет гостя.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		// НОВЫЙ МЕТОД: Завершить уборку
		public void FinishCleaning(int roomId)
		{
			NativeMethods.Hotel_FinishCleaning(_hotelPtr, roomId);
			RefreshData();
			_mainVm.SaveData();
			MessageBox.Show("Уборка завершена! Номер снова свободен.", "Успех");
		}

		public void UpdatePrice(int roomId, double newPrice)
		{
			NativeMethods.Hotel_UpdatePrice(_hotelPtr, roomId, newPrice);
			RefreshData();
			_mainVm.SaveData();
		}

		// И не забудь обновить CheckIn в ViewModel
		public void CheckIn(int roomId, string guestName, string passport, string inDate, string outDate, int days)
		{
			// Вызываем C++ с 6 аргументами
			bool success = NativeMethods.Hotel_CheckIn(_hotelPtr, roomId, guestName, passport, inDate, outDate, days);

			if (success)
			{
				RefreshData();
				_mainVm.SaveData(); // Сохраняем в файл
				MessageBox.Show($"Гость {guestName} успешно заселен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			else
			{
				MessageBox.Show("Не удалось заселить. Возможно, номер занят или на уборке.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}
	}
}