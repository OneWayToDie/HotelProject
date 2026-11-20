using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace HotelApp.Models
{
	// Статус записи в истории гостя
	public enum StayStatus
	{
		Past,    // Прошедшее проживание
		Current, // Проживает сейчас
		Booking  // Будущая бронь
	}

	// Одна запись истории проживания/брони гостя
	public class StayHistoryItem : INotifyPropertyChanged
	{
		public int RoomId { get; set; }
		public DateTime CheckIn { get; set; }
		public DateTime CheckOut { get; set; }
		public int Days { get; set; }
		public string BuildingName { get; set; } = string.Empty;
		public string RoomNumber { get; set; } = string.Empty;
		public string RoomTypeText { get; set; } = string.Empty;
		public double Revenue { get; set; }
		public StayStatus Status { get; set; }

		// Для отображения в таблице
		public string CheckInDisplay => CheckIn.ToString("dd.MM.yyyy");
		public string CheckOutDisplay => CheckOut.ToString("dd.MM.yyyy");
		public string DaysDisplay => $"{Days} дн.";
		public string RevenueDisplay => $"{Revenue:N0} ₽";
		public string RoomInfoDisplay => string.IsNullOrEmpty(BuildingName)
			? RoomNumber
			: $"{BuildingName}, {RoomNumber}";

		public string StatusText
		{
			get
			{
				switch (Status)
				{
					case StayStatus.Current: return "Проживает";
					case StayStatus.Booking: return "Бронь";
					default: return "Было";
				}
			}
		}

		public Brush StatusColor
		{
			get
			{
				switch (Status)
				{
					case StayStatus.Current: return Brushes.MediumSeaGreen;
					case StayStatus.Booking: return (Brush)Application.Current.FindResource("PrimaryHueMidBrush");
					default: return Brushes.Gray;
				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string name = null)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}
}