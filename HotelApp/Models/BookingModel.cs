namespace HotelApp.Models
{
	// Бронирование номера на конкретные даты
	public class BookingModel
	{
		public int Id { get; set; }
		public int RoomId { get; set; }
		public string GuestName { get; set; }
		public string Passport { get; set; }
		public string CheckInDate { get; set; }
		public string CheckOutDate { get; set; }

		public string DateDisplay => $"{CheckInDate} - {CheckOutDate}";
	}
}
