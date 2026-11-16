using System.Windows.Media;

namespace HotelApp.Models
{
	public enum LogType { Info, Success, Warning, Error }

	public class LogEntry
	{
		public string Time { get; set; }
		public string Message { get; set; }
		public LogType Type { get; set; }

		public Brush Color
		{
			get
			{
				switch (Type)
				{
					case LogType.Success: return Brushes.LightGreen;
					case LogType.Warning: return Brushes.Orange;
					case LogType.Error: return Brushes.IndianRed;
					default: return Brushes.LightGray;
				}
			}
		}
	}
}