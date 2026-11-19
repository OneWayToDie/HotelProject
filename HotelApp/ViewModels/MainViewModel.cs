using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using HotelApp.Models;
using HotelApp.Services;

namespace HotelApp.ViewModels
{
	public class MainViewModel : INotifyPropertyChanged
	{
		public IntPtr HotelPtr { get; private set; }
		public ObservableCollection<LogEntry> EventLog { get; } = new ObservableCollection<LogEntry>();
		public ObservableCollection<BuildingModel> Buildings { get; } = new ObservableCollection<BuildingModel>();

		private object _currentView;
		public object CurrentView
		{
			get => _currentView;
			set { _currentView = value; OnPropertyChanged(); }
		}

		// --- РЕАЛЬНАЯ СТАТИСТИКА ---
		private int _totalRooms;
		public int TotalRooms { get => _totalRooms; set { _totalRooms = value; OnPropertyChanged(); } }

		private int _freeRooms;
		public int FreeRooms { get => _freeRooms; set { _freeRooms = value; OnPropertyChanged(); } }

		private int _occupiedRooms;
		public double OccupancyPercent => TotalRooms > 0 ? (OccupiedRooms * 100.0 / TotalRooms) : 0;
		public int OccupiedRooms { get => _occupiedRooms; set { _occupiedRooms = value; OnPropertyChanged(); } }

		private double _revenueToday;
		public double RevenueToday { get => _revenueToday; set { _revenueToday = value; OnPropertyChanged(); } }

		// Путь к файлу сохранения (в папке с программой .exe)
		private readonly string _dataFile = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "hotel_data.txt");

		public MainViewModel()
		{
			try
			{
				HotelPtr = NativeMethods.Hotel_Create();

				// Пытаемся загрузить данные при старте
				if (NativeMethods.Hotel_Load(HotelPtr, _dataFile))
				{
					AddLog("Данные успешно загружены из файла.");
				}
				else
				{
					AddLog("Файл сохранения не найден. Создана новая база.");
				}
			}
			catch (Exception ex)
			{
				AddLog("Не удалось открыть базу данных: " + ex.Message, LogType.Error);
				AppDialog.Error("Не удалось открыть базу данных.\n" + ex.Message, "Запуск");
			}

			try
			{
				CurrentView = new RoomsViewModel(HotelPtr, this);
				UpdateStats();
				RefreshBuildings();
			}
			catch (Exception ex)
			{
				AddLog("Не удалось загрузить стартовый экран: " + ex.Message, LogType.Error);
			}
		}

		// Обновить список корпусов для диалога добавления номера
		public void RefreshBuildings()
		{
			try
			{
				var buffer = new CBuilding[100];
				int count = NativeMethods.Hotel_GetAllBuildings(HotelPtr, buffer, 100);

				Buildings.Clear();
				for (int i = 0; i < count; i++)
				{
					Buildings.Add(new BuildingModel { Id = buffer[i].Id, Name = buffer[i].Name });
				}
			}
			catch (Exception ex)
			{
				AddLog("Не удалось обновить список корпусов: " + ex.Message, LogType.Error);
			}
		}

		// МЕТОД ПЕРЕСЧЕТА СТАТИСТИКИ
		public void UpdateStats()
		{
			try
			{
				var buffer = new CRoom[1000];
				int count = NativeMethods.Hotel_GetAllRooms(HotelPtr, buffer, 1000);

				int free = 0, occupied = 0;
				for (int i = 0; i < count; i++)
				{
					if (buffer[i].Status == 0) free++; // 0 = Свободен
					else if (buffer[i].Status == 1) occupied++; // 1 = Занят
				}

				TotalRooms = count;
				FreeRooms = free;
				OccupiedRooms = occupied;
			}
			catch (Exception ex)
			{
				AddLog("Не удалось пересчитать статистику: " + ex.Message, LogType.Error);
			}

			OnPropertyChanged(nameof(OccupancyPercent));
		}

		// Проверка: не заняты ли выбранные даты чужой бронью
		public bool HasBookingConflict(int roomId, string inDate, string outDate, string guestName = null, string passport = null)
		{
			try
			{
				return CheckBookingConflict(roomId, inDate, outDate, guestName, passport);
			}
			catch (Exception ex)
			{
				AddLog("Не удалось проверить брони: " + ex.Message, LogType.Error);
				return false;
			}
		}

		private bool CheckBookingConflict(int roomId, string inDate, string outDate, string guestName, string passport)
		{
			var buffer = new CBooking[1000];
			int count = NativeMethods.Hotel_GetAllBookings(HotelPtr, buffer, 1000);

			for (int i = 0; i < count; i++)
			{
				if (buffer[i].RoomId != roomId) continue;

				// Заселение самого гостя, оформившего бронь, не блокируем
				bool ownBooking = false;
				if (!string.IsNullOrWhiteSpace(guestName) &&
					string.Equals(buffer[i].GuestName, guestName, StringComparison.OrdinalIgnoreCase))
					ownBooking = true;
				if (!ownBooking && !string.IsNullOrWhiteSpace(passport) &&
					string.Equals(buffer[i].Passport, passport, StringComparison.OrdinalIgnoreCase))
					ownBooking = true;

				if (ownBooking) continue;

				// Пересечение интервалов [inDate, outDate) и [bookingIn, bookingOut)
				if (string.CompareOrdinal(inDate, buffer[i].CheckOutDate) < 0 &&
					string.CompareOrdinal(buffer[i].CheckInDate, outDate) < 0)
					return true;
			}

			return false;
		}

		// ОБНОВЛЕННЫЙ МЕТОД ДОБАВЛЕНИЯ ЛОГА (принимает тип)
		public void AddLog(string message, LogType type = LogType.Info)
		{
			EventLog.Add(new LogEntry
			{
				Time = DateTime.Now.ToString("HH:mm:ss"),
				Message = message,
				Type = type
			});
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string name = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}

		public void SaveData()
		{
			try
			{
				if (NativeMethods.Hotel_Save(HotelPtr, _dataFile))
					AddLog("Данные сохранены в файл.");
				else
					AddLog("Не удалось сохранить данные в файл.", LogType.Error);
			}
			catch (Exception ex)
			{
				AddLog("Ошибка сохранения данных: " + ex.Message, LogType.Error);
			}
		}
	}
}