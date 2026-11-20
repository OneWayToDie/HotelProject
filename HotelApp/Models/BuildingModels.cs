using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HotelApp.Models
{
	public class BuildingModel : INotifyPropertyChanged
	{
		private int _id;
		private string _name;

		// Получает или задаёт идентификатор корпуса.
		public int Id
		{
			get => _id;
			set
			{
				_id = value;
				OnPropertyChanged();
			}
		}

		// Получает или задаёт название корпуса.
		public string Name
		{
			get => _name;
			set
			{
				_name = value;
				OnPropertyChanged();
			}
		}

		// Получает коллекцию номеров, принадлежащих корпусу.
		public ObservableCollection<RoomModel> Rooms { get; set; } = new ObservableCollection<RoomModel>();

		// Событие уведомления об изменении значения свойства.
		public event PropertyChangedEventHandler PropertyChanged;

		// Уведомляет подписчиков об изменении свойства.
		protected void OnPropertyChanged([CallerMemberName] string name = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}
	}
}
