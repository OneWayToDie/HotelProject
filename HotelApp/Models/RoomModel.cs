using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace HotelApp.Models
{
	public class RoomModel : INotifyPropertyChanged
	{
		private int _id;
		private string _number;
		private int _capacity;
		private double _basePrice;
		private int _status;
		private int _type;
		private string _guestName;

		public int Id { get => _id; set { _id = value; OnPropertyChanged(); } }
		public string Number { get => _number; set { _number = value; OnPropertyChanged(); } }
		public int Capacity { get => _capacity; set { _capacity = value; OnPropertyChanged(); } }
		public double BasePrice { get => _basePrice; set { _basePrice = value; OnPropertyChanged(); } }

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

		public int Type { get => _type; set { _type = value; OnPropertyChanged(); OnPropertyChanged(nameof(TypeText)); } }

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

		// Свойства для красивого отображения в таблице
		public string TypeText
		{
			get
			{
				if (Type == 1) return "Люкс";
				if (Type == 2) return "Конференц-зал";
				return "Стандарт";
			}
		}

		public string StatusText
		{
			get
			{
				if (Status == 1) return "Занят";
				if (Status == 2) return "На уборке";
				return "Свободен";
			}
		}

		public string DisplayGuest => string.IsNullOrEmpty(GuestName) ? "—" : GuestName;

		public Brush StatusColor
		{
			get
			{
				if (Status == 1) return Brushes.IndianRed; // Занят - Красный
				if (Status == 2) return Brushes.Orange;    // Уборка - Оранжевый
				return Brushes.MediumSeaGreen;             // Свободен - Зеленый
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string name = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}

		private string _checkInDate;
		public string CheckInDate { get => _checkInDate; set { _checkInDate = value; OnPropertyChanged(); OnPropertyChanged(nameof(DateDisplay)); } }

		private string _checkOutDate;
		public string CheckOutDate { get => _checkOutDate; set { _checkOutDate = value; OnPropertyChanged(); OnPropertyChanged(nameof(DateDisplay)); } }

		public string DateDisplay => string.IsNullOrEmpty(CheckInDate) ? "—" : $"{CheckInDate} - {CheckOutDate}";
	}
}