using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using HotelApp.Services;

namespace HotelApp.ViewModels
{
	public class MainViewModel : INotifyPropertyChanged
	{
		public IntPtr HotelPtr { get; private set; }
		public ObservableCollection<string> EventLog { get; } = new ObservableCollection<string>();

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
		public int OccupiedRooms { get => _occupiedRooms; set { _occupiedRooms = value; OnPropertyChanged(); } }

		private double _revenueToday;
		public double RevenueToday { get => _revenueToday; set { _revenueToday = value; OnPropertyChanged(); } }

		// Путь к файлу сохранения (в папке с программой .exe)
		private readonly string _dataFile = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "hotel_data.txt");

		public MainViewModel()
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

			CurrentView = new RoomsViewModel(HotelPtr, this);
			UpdateStats();
		}

		// МЕТОД ПЕРЕСЧЕТА СТАТИСТИКИ
		public void UpdateStats()
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

		public void AddLog(string message)
		{
			string timestamp = DateTime.Now.ToString("HH:mm:ss");
			EventLog.Add($"[{timestamp}] {message}");
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string name = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}

		public void SaveData()
		{
			if (NativeMethods.Hotel_Save(HotelPtr, _dataFile))
				AddLog("Данные сохранены в файл.");
		}
	}
}