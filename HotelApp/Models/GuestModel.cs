using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HotelApp.Models
{
	public class GuestModel : INotifyPropertyChanged
	{
		private int _id;
		private string _fullName;
		private string _passport;

		public int Id { get => _id; set { _id = value; OnPropertyChanged(); } }
		public string FullName { get => _fullName; set { _fullName = value; OnPropertyChanged(); } }
		public string Passport { get => _passport; set { _passport = value; OnPropertyChanged(); } }

		private int _rowNumber;
		public int RowNumber
		{
			get => _rowNumber;
			set { _rowNumber = value; OnPropertyChanged(); }
		}

		// --- НОВЫЕ СВОЙСТВА ДЛЯ АГРЕГАЦИИ ---

		// Суммарное время проживания (все заезды + брони)
		private int _totalDays;
		public int TotalDays
		{
			get => _totalDays;
			set { _totalDays = value; OnPropertyChanged(); }
		}
		public string TotalDaysDisplay => _totalDays > 0 ? $"{_totalDays} дн." : "—";

		// Суммарная прибыль по гостю
		private double _totalRevenue;
		public double TotalRevenue
		{
			get => _totalRevenue;
			set { _totalRevenue = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalRevenueDisplay)); }
		}
		public string TotalRevenueDisplay =>
			_totalRevenue > 0 ? $"{_totalRevenue:N0} ₽" : "—";

		// Последний/текущий номер (для основной строки)
		private string _lastRoomInfo = "—";
		public string LastRoomInfo
		{
			get => _lastRoomInfo;
			set { _lastRoomInfo = value; OnPropertyChanged(); }
		}

		// Коллекция для RowDetails (раскрывающаяся панель)
		public ObservableCollection<StayHistoryItem> StayHistory { get; }
			= new ObservableCollection<StayHistoryItem>();

		// --- СТАРЫЕ СВОЙСТВА (оставляем для совместимости, если где-то используются) ---
		private string _stayDisplay = "—";
		public string StayDisplay
		{
			get => _stayDisplay;
			set { _stayDisplay = value; OnPropertyChanged(); }
		}
		private string _stayToolTip;
		public string StayToolTip
		{
			get => _stayToolTip;
			set { _stayToolTip = value; OnPropertyChanged(); }
		}
		private string _bookingDisplay = "—";
		public string BookingDisplay
		{
			get => _bookingDisplay;
			set { _bookingDisplay = value; OnPropertyChanged(); }
		}
		private string _bookingToolTip;
		public string BookingToolTip
		{
			get => _bookingToolTip;
			set { _bookingToolTip = value; OnPropertyChanged(); }
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string name = null)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}
}