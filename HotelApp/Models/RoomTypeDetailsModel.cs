namespace HotelApp.Models
{
	public class RoomTypeDetailsModel
	{
		// Получает или задаёт имя гостя в детализации типа номера.
		public string GuestName { get; set; }

		// Получает или задаёт паспортные данные гостя.
		public string Passport { get; set; }

		// Получает или задаёт номер комнаты.
		public string RoomNumber { get; set; }

		// Получает или задаёт строковое представление периода проживания.
		public string DatesDisplay { get; set; }
	}
}
