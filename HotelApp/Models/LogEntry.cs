using System.Windows.Media;

namespace HotelApp.Models
{
	public enum LogType
	{
		Info,
		Success,
		Warning,
		Error
	}

	public class LogEntry
	{
		// Получает или задаёт время записи в журнале.
		public string Time { get; set; }

		// Получает или задаёт текст сообщения журнала.
		public string Message { get; set; }

		// Получает или задаёт тип записи журнала.
		public LogType Type { get; set; }

		// Возвращает цвет отображения записи в зависимости от её типа.
		public Brush Color
		{
			get
			{
				switch (Type)
				{
					case LogType.Success:
						return Brushes.LightGreen;
					case LogType.Warning:
						return Brushes.Orange;
					case LogType.Error:
						return Brushes.IndianRed;
					default:
						return Brushes.LightGray;
				}
			}
		}
	}
}