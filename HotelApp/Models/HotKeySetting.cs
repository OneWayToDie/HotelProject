using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HotelApp.Models
{
	public class HotkeySetting : INotifyPropertyChanged
	{
		public string ActionKey { get; set; }
		public string DisplayName { get; set; }

		private string _gesture;
		public string Gesture
		{
			get => _gesture;
			set { _gesture = value; OnPropertyChanged(); }
		}

		private bool _isRecording;
		public bool IsRecording
		{
			get => _isRecording;
			set
			{
				_isRecording = value;
				OnPropertyChanged();
				// При начале записи очищаем поле
				if (value) Gesture = "";
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string name = null)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}
}