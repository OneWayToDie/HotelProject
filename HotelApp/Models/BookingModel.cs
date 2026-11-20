namespace HotelApp.Models
{
	public class BookingModel
	{
		// Получает или задаёт идентификатор бронирования.
		public int Id { get; set; }

		// Получает или задаёт идентификатор номера.
		public int RoomId { get; set; }

		// Получает или задаёт имя гостя.
		public string GuestName { get; set; }

		// Получает или задаёт паспортные данные гостя.
		public string Passport { get; set; }

		// Получает или задаёт дату заезда.
		public string CheckInDate { get; set; }

		// Получает или задаёт дату выезда.
		public string CheckOutDate { get; set; }

		// Возвращает строковое представление периода бронирования.
		public string DateDisplay => $"{CheckInDate} - {CheckOutDate}";
	}
}
