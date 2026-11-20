using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace HotelApp.Models
{
	public class RoomModel : INotifyPropertyChanged
	{
		private int _id;
		private int _buildingId;
		private string _number;
		private int _capacity;
		private double _basePrice;
		private int _status;
		private int _type;
		private string _guestName;
		private bool _isWaitingForCheckIn;
		private string _checkInDate;
		private string _checkOutDate;
		private string _bookingInfo = "—";

		// Получает или задаёт идентификатор номера.
		public int Id
		{
			get => _id;
			set
			{
				_id = value;
				OnPropertyChanged();
			}
		}

		// Получает или задаёт идентификатор корпуса.
		public int BuildingId
		{
			get => _buildingId;
			set
			{
				_buildingId = value;
				OnPropertyChanged();
			}
		}

		// Получает или задаёт отображаемый номер комнаты.
		public string Number
		{
			get => _number;
			set
			{
				_number = value;
				OnPropertyChanged();
			}
		}

		// Получает или задаёт вместимость номера.
		public int Capacity
		{
			get => _capacity;
			set
			{
				_capacity = value;
				OnPropertyChanged();
			}
		}

		// Получает или задаёт базовую цену номера.
		public double BasePrice
		{
			get => _basePrice;
			set
			{
				_basePrice = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(PriceLabel));
			}
		}

		// Получает или задаёт статус номера.
		public int Status
		{
			get => _status;
			set
			{
				_status = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(StatusText));
				OnPropertyChanged(nameof(StatusColor));
			}
		}

		// Получает или задаёт тип номера.
		public int Type
		{
			get => _type;
			set
			{
				_type = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(TypeText));
				OnPropertyChanged(nameof(IsLux));
			}
		}

		// Получает или задаёт имя текущего гостя.
		public string GuestName
		{
			get => _guestName;
			set
			{
				_guestName = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(DisplayGuest));
			}
		}

		// Получает или задаёт признак ожидания заселения по бронированию.
		public bool IsWaitingForCheckIn
		{
			get => _isWaitingForCheckIn;
			set
			{
				_isWaitingForCheckIn = value;
				OnPropertyChanged();
			}
		}

		// Получает или задаёт дату заезда текущего проживания.
		public string CheckInDate
		{
			get => _checkInDate;
			set
			{
				_checkInDate = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(DateDisplay));
			}
		}

		// Получает или задаёт дату выезда текущего проживания.
		public string CheckOutDate
		{
			get => _checkOutDate;
			set
			{
				_checkOutDate = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(DateDisplay));
			}
		}

		// Получает или задаёт информацию о ближайшем бронировании номера.
		public string BookingInfo
		{
			get => _bookingInfo;
			set
			{
				_bookingInfo = value;
				OnPropertyChanged();
			}
		}

		// Возвращает текстовое представление типа номера.
		public string TypeText
		{
			get
			{
				if (Type == 1) return "Люкс";
				if (Type == 2) return "Конференц-зал";
				return "Стандарт";
			}
		}

		// Возвращает текстовое представление статуса номера.
		public string StatusText
		{
			get
			{
				if (Status == 1) return "Занят";
				if (Status == 2) return "На уборке";
				return "Свободен";
			}
		}

		// Возвращает имя гостя для отображения в интерфейсе.
		public string DisplayGuest => string.IsNullOrEmpty(GuestName) ? "—" : GuestName;

		// Возвращает признак того, что номер относится к типу «Люкс».
		public bool IsLux => Type == 1;

		// Возвращает цену номера в формате отображения.
		public string PriceLabel => $"{BasePrice:N0} ₽";

		// Возвращает цвет статуса номера для отображения.
		public Brush StatusColor
		{
			get
			{
				if (Status == 1) return Brushes.IndianRed;
				if (Status == 2) return Brushes.Orange;
				return Brushes.MediumSeaGreen;
			}
		}

		// Возвращает период проживания в формате отображения.
		public string DateDisplay => string.IsNullOrEmpty(CheckInDate) ? "—" : $"{CheckInDate} - {CheckOutDate}";

		// Событие уведомления об изменении значения свойства.
		public event PropertyChangedEventHandler PropertyChanged;

		// Уведомляет подписчиков об изменении свойства.
		protected void OnPropertyChanged([CallerMemberName] string name = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}
	}
}
