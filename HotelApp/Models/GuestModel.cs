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
		private int _rowNumber;
		private int _totalDays;
		private double _totalRevenue;
		private bool _isVip;
		private string _lastRoomInfo = "—";
		private string _stayDisplay = "—";
		private string _stayToolTip;
		private string _bookingDisplay = "—";
		private string _bookingToolTip;

		// Получает или задаёт идентификатор гостя.
		public int Id
		{
			get => _id;
			set
			{
				_id = value;
				OnPropertyChanged();
			}
		}

		// Получает или задаёт полное имя гостя.
		public string FullName
		{
			get => _fullName;
			set
			{
				_fullName = value;
				OnPropertyChanged();
			}
		}

		// Получает или задаёт паспортные данные гостя.
		public string Passport
		{
			get => _passport;
			set
			{
				_passport = value;
				OnPropertyChanged();
			}
		}

		// Получает или задаёт порядковый номер строки в таблице.
		public int RowNumber
		{
			get => _rowNumber;
			set
			{
				_rowNumber = value;
				OnPropertyChanged();
			}
		}

		// Получает или задаёт суммарное количество дней проживания.
		public int TotalDays
		{
			get => _totalDays;
			set
			{
				_totalDays = value;
				OnPropertyChanged();
			}
		}

		// Возвращает суммарное количество дней в формате отображения.
		public string TotalDaysDisplay => _totalDays > 0 ? $"{_totalDays} дн." : "—";

		// Получает или задаёт суммарную выручку по гостю.
		public double TotalRevenue
		{
			get => _totalRevenue;
			set
			{
				_totalRevenue = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(TotalRevenueDisplay));
			}
		}

		// Возвращает суммарную выручку в формате отображения.
		public string TotalRevenueDisplay =>
			_totalRevenue > 0 ? $"{_totalRevenue:N0} ₽" : "—";

		// Получает или задаёт признак VIP-статуса гостя.
		public bool IsVip
		{
			get => _isVip;
			set
			{
				if (_isVip == value) return;
				_isVip = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(VipToolTip));
			}
		}

		// Возвращает текст подсказки для VIP-гостя.
		public string VipToolTip => _isVip ? "VIP-гость" : null;

		// Получает или задаёт информацию о последнем или текущем номере гостя.
		public string LastRoomInfo
		{
			get => _lastRoomInfo;
			set
			{
				_lastRoomInfo = value;
				OnPropertyChanged();
			}
		}

		// Получает коллекцию записей истории проживаний и бронирований.
		public ObservableCollection<StayHistoryItem> StayHistory { get; }
			= new ObservableCollection<StayHistoryItem>();

		// Получает или задаёт отображаемую информацию о проживании.
		public string StayDisplay
		{
			get => _stayDisplay;
			set
			{
				_stayDisplay = value;
				OnPropertyChanged();
			}
		}

		// Получает или задаёт подсказку для информации о проживании.
		public string StayToolTip
		{
			get => _stayToolTip;
			set
			{
				_stayToolTip = value;
				OnPropertyChanged();
			}
		}

		// Получает или задаёт отображаемую информацию о бронировании.
		public string BookingDisplay
		{
			get => _bookingDisplay;
			set
			{
				_bookingDisplay = value;
				OnPropertyChanged();
			}
		}

		// Получает или задаёт подсказку для информации о бронировании.
		public string BookingToolTip
		{
			get => _bookingToolTip;
			set
			{
				_bookingToolTip = value;
				OnPropertyChanged();
			}
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
