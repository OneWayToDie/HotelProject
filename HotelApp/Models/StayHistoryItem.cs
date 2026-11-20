using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace HotelApp.Models
{
	public enum StayStatus
	{
		Past,
		Current,
		Booking
	}

	public class StayHistoryItem : INotifyPropertyChanged
	{
		// Получает или задаёт идентификатор номера проживания.
		public int RoomId { get; set; }

		// Получает или задаёт дату заезда.
		public DateTime CheckIn { get; set; }

		// Получает или задаёт дату выезда.
		public DateTime CheckOut { get; set; }

		// Получает или задаёт количество дней проживания.
		public int Days { get; set; }

		// Получает или задаёт название корпуса.
		public string BuildingName { get; set; } = string.Empty;

		// Получает или задаёт номер комнаты.
		public string RoomNumber { get; set; } = string.Empty;

		// Получает или задаёт текстовое представление типа номера.
		public string RoomTypeText { get; set; } = string.Empty;

		// Получает или задаёт сумму проживания или бронирования.
		public double Revenue { get; set; }

		// Получает или задаёт статус записи истории.
		public StayStatus Status { get; set; }

		// Возвращает дату заезда в формате отображения.
		public string CheckInDisplay => CheckIn.ToString("dd.MM.yyyy");

		// Возвращает дату выезда в формате отображения.
		public string CheckOutDisplay => CheckOut.ToString("dd.MM.yyyy");

		// Возвращает количество дней в формате отображения.
		public string DaysDisplay => $"{Days} дн.";

		// Возвращает сумму в формате отображения.
		public string RevenueDisplay => $"{Revenue:N0} ₽";

		// Возвращает информацию о корпусе и номере для отображения.
		public string RoomInfoDisplay => string.IsNullOrEmpty(BuildingName)
			? RoomNumber
			: $"{BuildingName}, {RoomNumber}";

		// Возвращает текстовое представление статуса записи.
		public string StatusText
		{
			get
			{
				switch (Status)
				{
					case StayStatus.Current:
						return "Проживает";
					case StayStatus.Booking:
						return "Бронь";
					default:
						return "Было";
				}
			}
		}

		// Возвращает цвет статуса записи для отображения в интерфейсе.
		public Brush StatusColor
		{
			get
			{
				switch (Status)
				{
					case StayStatus.Current:
						return Brushes.MediumSeaGreen;
					case StayStatus.Booking:
						return (Brush)Application.Current.FindResource("PrimaryHueMidBrush");
					default:
						return Brushes.Gray;
				}
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