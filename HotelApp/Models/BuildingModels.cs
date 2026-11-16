using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HotelApp.Models
{
	public class BuildingModel : INotifyPropertyChanged
	{
		private int _id;
		private string _name;

		public int Id { get => _id; set { _id = value; OnPropertyChanged(); } }
		public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }

		// Сюда мы будем складывать номера этого корпуса (для дерева)
		public ObservableCollection<RoomModel> Rooms { get; set; } = new ObservableCollection<RoomModel>();

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string name = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}
	}
}
