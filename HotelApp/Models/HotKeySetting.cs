using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HotelApp.Models
{
	public class HotkeySetting : INotifyPropertyChanged
	{
		private string _gesture;
		private bool _isRecording;

		// Получает или задаёт ключ действия для горячей клавиши.
		public string ActionKey { get; set; }

		// Получает или задаёт отображаемое название действия.
		public string DisplayName { get; set; }

		// Получает или задаёт строковое представление комбинации клавиш.
		public string Gesture
		{
			get => _gesture;
			set
			{
				_gesture = value;
				OnPropertyChanged();
			}
		}

		// Получает или задаёт признак режима записи сочетания клавиш.
		public bool IsRecording
		{
			get => _isRecording;
			set
			{
				_isRecording = value;
				OnPropertyChanged();
			}
		}

		// Событие уведомления об изменении значения свойства.
		public event PropertyChangedEventHandler PropertyChanged;

		// Уведомляет подписчиков об изменении свойства.
		protected void OnPropertyChanged([CallerMemberName] string name = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}
	}
}