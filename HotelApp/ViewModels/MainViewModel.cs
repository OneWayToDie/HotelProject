using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using HotelApp.Models;
using HotelApp.Services;

namespace HotelApp.ViewModels
{
	public class MainViewModel : INotifyPropertyChanged
	{
		private static readonly Brush DatabaseOkBrush = CreateFrozenBrush(Color.FromRgb(0x2E, 0x7D, 0x32));
		private static readonly Brush DatabaseErrorBrush = CreateFrozenBrush(Color.FromRgb(0xC6, 0x28, 0x28));
		private static readonly Brush DatabaseNewBrush = CreateFrozenBrush(Color.FromRgb(0x9E, 0x9E, 0x9E));

		private readonly string _dataFile = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "hotel_data.txt");
		private object _currentView;
		private int _totalRooms;
		private int _freeRooms;
		private int _occupiedRooms;
		private double _revenueToday;
		private bool? _isDatabaseOk;

		// Получает указатель на нативный объект отеля.
		public IntPtr HotelPtr { get; private set; }

		// Получает журнал событий приложения.
		public ObservableCollection<LogEntry> EventLog { get; } = new ObservableCollection<LogEntry>();

		// Получает коллекцию корпусов для использования в интерфейсе.
		public ObservableCollection<BuildingModel> Buildings { get; } = new ObservableCollection<BuildingModel>();

		// Получает или задаёт текущее активное представление.
		public object CurrentView
		{
			get => _currentView;
			set
			{
				_currentView = value;
				OnPropertyChanged();
			}
		}

		// Получает или задаёт общее количество номеров.
		public int TotalRooms
		{
			get => _totalRooms;
			set
			{
				_totalRooms = value;
				OnPropertyChanged();
			}
		}

		// Получает или задаёт количество свободных номеров.
		public int FreeRooms
		{
			get => _freeRooms;
			set
			{
				_freeRooms = value;
				OnPropertyChanged();
			}
		}

		// Получает или задаёт количество занятых номеров.
		public int OccupiedRooms
		{
			get => _occupiedRooms;
			set
			{
				_occupiedRooms = value;
				OnPropertyChanged();
			}
		}

		// Возвращает процент загрузки отеля.
		public double OccupancyPercent => TotalRooms > 0 ? (OccupiedRooms * 100.0 / TotalRooms) : 0;

		// Получает или задаёт значение выручки за текущий день.
		public double RevenueToday
		{
			get => _revenueToday;
			set
			{
				_revenueToday = value;
				OnPropertyChanged();
			}
		}

		// Получает текущее состояние подключения базы данных.
		public bool? IsDatabaseOk
		{
			get => _isDatabaseOk;
			private set
			{
				if (_isDatabaseOk == value) return;
				_isDatabaseOk = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(DatabaseStatusBrush));
				OnPropertyChanged(nameof(DatabaseStatusText));
				OnPropertyChanged(nameof(DatabaseStatusToolTip));
			}
		}

		// Возвращает цвет индикатора состояния базы данных.
		public Brush DatabaseStatusBrush =>
			_isDatabaseOk == true ? DatabaseOkBrush :
			_isDatabaseOk == false ? DatabaseErrorBrush : DatabaseNewBrush;

		// Возвращает текст индикатора состояния базы данных.
		public string DatabaseStatusText =>
			_isDatabaseOk == true ? "БД: подключена" :
			_isDatabaseOk == false ? "БД: ошибка" : "БД: новая";

		// Возвращает подсказку для индикатора состояния базы данных.
		public string DatabaseStatusToolTip =>
			_isDatabaseOk == true ? "База данных загружена, сохранение работает.\n" + _dataFile :
			_isDatabaseOk == false ? "Ошибка доступа к базе данных. Подробности — в журнале.\n" + _dataFile :
			"Файл сохранения ещё не создан — данные сохранятся при первом изменении.\n" + _dataFile;

		// Инициализирует главную модель представления и загружает данные приложения.
		public MainViewModel()
		{
			try
			{
				HotelPtr = NativeMethods.Hotel_Create();

				if (NativeMethods.Hotel_Load(HotelPtr, _dataFile))
				{
					AddLog("Данные успешно загружены из файла.");
					IsDatabaseOk = true;
				}
				else
				{
					AddLog("Файл сохранения не найден. Создана новая база.");
					IsDatabaseOk = null;
				}
			}
			catch (Exception ex)
			{
				IsDatabaseOk = false;
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

		// Обновляет список корпусов для использования в диалогах и представлениях.
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

		// Пересчитывает сводные показатели по номерам отеля.
		public void UpdateStats()
		{
			try
			{
				var buffer = new CRoom[1000];
				int count = NativeMethods.Hotel_GetAllRooms(HotelPtr, buffer, 1000);

				int free = 0, occupied = 0;
				for (int i = 0; i < count; i++)
				{
					if (buffer[i].Status == 0) free++;
					else if (buffer[i].Status == 1) occupied++;
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

		// Проверяет наличие конфликтов бронирования для указанного номера и периода.
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

		// Добавляет запись в журнал событий приложения.
		public void AddLog(string message, LogType type = LogType.Info)
		{
			EventLog.Add(new LogEntry
			{
				Time = DateTime.Now.ToString("HH:mm:ss"),
				Message = message,
				Type = type
			});
		}

		// Сохраняет текущее состояние данных отеля в файл.
		public void SaveData()
		{
			try
			{
				if (NativeMethods.Hotel_Save(HotelPtr, _dataFile))
				{
					AddLog("Данные сохранены в файл.");
					IsDatabaseOk = true;
				}
				else
				{
					AddLog("Не удалось сохранить данные в файл.", LogType.Error);
					IsDatabaseOk = false;
				}
			}
			catch (Exception ex)
			{
				IsDatabaseOk = false;
				AddLog("Ошибка сохранения данных: " + ex.Message, LogType.Error);
			}
		}

		// Выполняет проверку пересечения дат бронирований для указанного номера.
		private bool CheckBookingConflict(int roomId, string inDate, string outDate, string guestName, string passport)
		{
			var buffer = new CBooking[1000];
			int count = NativeMethods.Hotel_GetAllBookings(HotelPtr, buffer, 1000);

			for (int i = 0; i < count; i++)
			{
				if (buffer[i].RoomId != roomId) continue;

				bool ownBooking = false;
				if (!string.IsNullOrWhiteSpace(guestName) &&
					string.Equals(buffer[i].GuestName, guestName, StringComparison.OrdinalIgnoreCase))
					ownBooking = true;
				if (!ownBooking && !string.IsNullOrWhiteSpace(passport) &&
					string.Equals(buffer[i].Passport, passport, StringComparison.OrdinalIgnoreCase))
					ownBooking = true;

				if (ownBooking) continue;

				if (string.CompareOrdinal(inDate, buffer[i].CheckOutDate) < 0 &&
					string.CompareOrdinal(buffer[i].CheckInDate, outDate) < 0)
					return true;
			}

			return false;
		}

		// Создаёт замороженную кисть указанного цвета.
		private static Brush CreateFrozenBrush(Color color)
		{
			var brush = new SolidColorBrush(color);
			brush.Freeze();
			return brush;
		}

		// Событие уведомления об изменении значения свойства.
		public event PropertyChangedEventHandler PropertyChanged;

		// Уведомляет подписчиков об изменении свойства.
		protected void OnPropertyChanged([CallerMemberName] string name = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}
	}
}
