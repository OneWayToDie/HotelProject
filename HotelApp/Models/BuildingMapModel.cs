using System.Collections.ObjectModel;

namespace HotelApp.Models
{
	public class BuildingMapModel
	{
		// Получает или задаёт идентификатор корпуса.
		public int BuildingId { get; set; }

		// Получает или задаёт название корпуса.
		public string BuildingName { get; set; }

		// Получает коллекцию номеров корпуса для отображения на карте.
		public ObservableCollection<RoomModel> Rooms { get; set; } = new ObservableCollection<RoomModel>();
	}
}
