using System.Collections.ObjectModel;

namespace HotelApp.Models
{
	// Группа номеров одного корпуса для "Карты отеля"
	public class BuildingMapModel
	{
		public int BuildingId { get; set; }
		public string BuildingName { get; set; }
		public ObservableCollection<RoomModel> Rooms { get; set; } = new ObservableCollection<RoomModel>();
	}
}
