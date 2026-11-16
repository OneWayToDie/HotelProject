using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HotelApp.Models
{
	public class GuestModel : INotifyPropertyChanged
	{
		private int _id;
		private string _fullName;
		private string _passport;

		public int Id { get => _id; set { _id = value; OnPropertyChanged(); } }
		public string FullName { get => _fullName; set { _fullName = value; OnPropertyChanged(); } }
		public string Passport { get => _passport; set { _passport = value; OnPropertyChanged(); } }

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string name = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}
	}
}