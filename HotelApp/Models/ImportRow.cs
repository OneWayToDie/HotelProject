using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HotelApp.Models
{
	// Одна строка Excel-файла с гостями (до и после проверки)
	public class ImportRow : INotifyPropertyChanged
	{
		public int LineNumber { get; set; }

		// Сырые значения из файла
		public string FullName { get; set; } = string.Empty;
		public string Passport { get; set; } = string.Empty;
		public string Building { get; set; } = string.Empty;
		public string RoomType { get; set; } = string.Empty;
		public string RoomNumber { get; set; } = string.Empty;
		public string PriceText { get; set; } = string.Empty;
		public string CheckInText { get; set; } = string.Empty;
		public string CheckOutText { get; set; } = string.Empty;

		// Разобранные значения (заполняются после проверки)
		public double Price { get; set; }
		public int RoomTypeValue { get; set; }
		public DateTime CheckIn { get; set; }
		public DateTime CheckOut { get; set; }

		private string _error = string.Empty;
		public string Error
		{
			get => _error;
			set
			{
				_error = value ?? string.Empty;
				OnPropertyChanged();
				OnPropertyChanged(nameof(HasError));
				OnPropertyChanged(nameof(Status));
			}
		}

		public bool HasError => !string.IsNullOrEmpty(_error);
		public string Status => HasError ? _error : "OK";
		public string DatesDisplay => $"{CheckInText} - {CheckOutText}";
		public string RoomDisplay => $"{Building} / {RoomNumber}";

		// Ключ номера: корпус + номер (без учета регистра)
		public string RoomKey =>
			(Building ?? string.Empty).Trim().ToLowerInvariant() + "|" +
			(RoomNumber ?? string.Empty).Trim().ToLowerInvariant();

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string name = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}
	}
}
