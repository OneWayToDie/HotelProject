//using System;
//using System.Collections.ObjectModel;
//using System.ComponentModel;
//using System.Windows;
//using HotelApp.Models;
//using HotelApp.Services;
//using static HotelApp.Services.NativeMethods;

//namespace HotelApp.ViewModels
//{
//	public class HotelViewModel : INotifyPropertyChanged
//	{
//		private IntPtr _hotelPtr;
		

//		public ObservableCollection<BuildingModel> Buildings { get; } = new ObservableCollection<BuildingModel>();
//		public ObservableCollection<RoomModel> Rooms { get; } = new ObservableCollection<RoomModel>();

//		private BuildingModel _selectedBuilding;
//		public BuildingModel SelectedBuilding
//		{
//			get => _selectedBuilding;
//			set
//			{
//				_selectedBuilding = value;
//				OnPropertyChanged();
//				RefreshRooms(); // При смене корпуса обновляем таблицу
//			}
//		}

//		public HotelViewModel()
//		{
//			_hotelPtr = NativeMethods.Hotel_Create();

//			// Добавляем тестовый корпус и номера
//			NativeMethods.Hotel_AddBuilding(_hotelPtr, "Главный корпус");
//			NativeMethods.Hotel_AddBuilding(_hotelPtr, "Корпус Б");

//			NativeMethods.Hotel_AddRoom(_hotelPtr, 1, 0, "101");
//			NativeMethods.Hotel_AddRoom(_hotelPtr, 1, 1, "102");
//			NativeMethods.Hotel_AddRoom(_hotelPtr, 2, 2, "201");

//			RefreshData();
//		}

//		public void RefreshData()
//		{
//			// 1. Загружаем здания
//			var bBuffer = new CBuilding[100];
//			int bCount = NativeMethods.Hotel_GetAllBuildings(_hotelPtr, bBuffer, 100);

//			Buildings.Clear();
//			for (int i = 0; i < bCount; i++)
//			{
//				Buildings.Add(new BuildingModel { Id = bBuffer[i].Id, Name = bBuffer[i].Name });
//			}

//			// Если ничего не выбрано, выбираем первое
//			if (SelectedBuilding == null && Buildings.Count > 0)
//				SelectedBuilding = Buildings[0];
//			else
//				RefreshRooms();
//		}

//		private void RefreshRooms()
//		{
//			if (SelectedBuilding == null) return;

//			var buffer = new CRoom[100];
//			int count = NativeMethods.Hotel_GetAllRooms(_hotelPtr, buffer, 100);

//			Rooms.Clear();
//			for (int i = 0; i < count; i++)
//			{
//				// В этой простой версии мы показываем все номера, 
//				// так как в C++ у нас нет привязки room->buildingId в структуре CRoom. 
//				// Для курсовой этого достаточно, но можно добавить фильтр.
//				Rooms.Add(new RoomModel
//				{
//					Id = buffer[i].Id,
//					Number = buffer[i].Number,
//					Capacity = buffer[i].Capacity,
//					BasePrice = buffer[i].BasePrice,
//					Status = buffer[i].Status,
//					Type = buffer[i].Type,
//					GuestName = buffer[i].GuestName
//				});
//			}
//		}

//		// ПРАВИЛЬНЫЙ ВАРИАНТ
//		public void CheckIn(int roomId, string guestName, string passport, string inDate, string outDate, int days)
//		{
//			// Передаем 6 аргументов, включая даты
//			bool success = NativeMethods.Hotel_CheckIn(_hotelPtr, roomId, guestName, passport, inDate, outDate, days);

//			if (success)
//			{
//				RefreshData();
//				_mainVm.SaveData(); // Сохраняем в файл
//				MessageBox.Show($"Гость {guestName} успешно заселен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
//			}
//			else
//			{
//				MessageBox.Show("Не удалось заселить. Возможно, номер занят или на уборке.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
//			}
//		}

//		public void CheckOut(RoomModel room)
//		{
//			if (room == null) return;
//			double price = NativeMethods.Hotel_CheckOut(_hotelPtr, room.Id);
//			if (price > 0)
//			{
//				RefreshData();
//				MessageBox.Show($"Выселен. Оплата: {price:C}");
//			}
//		}

//		public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
//		protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string name = null)
//		{
//			PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
//		}
//	}
//}