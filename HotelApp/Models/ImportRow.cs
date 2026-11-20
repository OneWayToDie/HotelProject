using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HotelApp.Models
{
	public class ImportRow : INotifyPropertyChanged
	{
		private string _error = string.Empty;

		// Получает или задаёт номер строки в исходном файле.
		public int LineNumber { get; set; }

		// Получает или задаёт ФИО гостя из исходного файла.
		public string FullName { get; set; } = string.Empty;

		// Получает или задаёт паспортные данные из исходного файла.
		public string Passport { get; set; } = string.Empty;

		// Получает или задаёт название корпуса из исходного файла.
		public string Building { get; set; } = string.Empty;

		// Получает или задаёт тип номера из исходного файла.
		public string RoomType { get; set; } = string.Empty;

		// Получает или задаёт номер комнаты из исходного файла.
		public string RoomNumber { get; set; } = string.Empty;

		// Получает или задаёт текстовое значение цены из исходного файла.
		public string PriceText { get; set; } = string.Empty;

		// Получает или задаёт текстовое значение даты заезда из исходного файла.
		public string CheckInText { get; set; } = string.Empty;

		// Получает или задаёт текстовое значение даты выезда из исходного файла.
		public string CheckOutText { get; set; } = string.Empty;

		// Получает или задаёт разобранное числовое значение цены.
		public double Price { get; set; }

		// Получает или задаёт числовое представление типа номера.
		public int RoomTypeValue { get; set; }

		// Получает или задаёт разобранную дату заезда.
		public DateTime CheckIn { get; set; }

		// Получает или задаёт разобранную дату выезда.
		public DateTime CheckOut { get; set; }

		// Получает или задаёт текст ошибки проверки строки импорта.
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

		// Возвращает признак наличия ошибки в строке импорта.
		public bool HasError => !string.IsNullOrEmpty(_error);

		// Возвращает статус строки импорта для отображения пользователю.
		public string Status => HasError ? _error : "OK";

		// Возвращает строковое представление диапазона дат из исходных данных.
		public string DatesDisplay => $"{CheckInText} - {CheckOutText}";

		// Возвращает строковое представление корпуса и номера комнаты.
		public string RoomDisplay => $"{Building} / {RoomNumber}";

		// Возвращает нормализованный ключ номера для проверки совпадений.
		public string RoomKey =>
			(Building ?? string.Empty).Trim().ToLowerInvariant() + "|" +
			(RoomNumber ?? string.Empty).Trim().ToLowerInvariant();

		// Событие уведомления об изменении значения свойства.
		public event PropertyChangedEventHandler PropertyChanged;

		// Уведомляет подписчиков об изменении свойства.
		protected void OnPropertyChanged([CallerMemberName] string name = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}
	}
}
