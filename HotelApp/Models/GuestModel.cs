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

		// Номер строки в текущем (отфильтрованном) списке. Пересчитывается после удаления.
		private int _rowNumber;
		public int RowNumber
		{
			get => _rowNumber;
			set { _rowNumber = value; OnPropertyChanged(); }
		}

		// Ближайшее проживание: "01.12.2026 — 05.12.2026" или "—"
		private string _stayDisplay = "—";
		public string StayDisplay
		{
			get => _stayDisplay;
			set { _stayDisplay = value; OnPropertyChanged(); }
		}

		// Полный список проживаний для всплывающей подсказки (null — подсказка не нужна)
		private string _stayToolTip;
		public string StayToolTip
		{
			get => _stayToolTip;
			set { _stayToolTip = value; OnPropertyChanged(); }
		}

		// Ближайшая бронь: "01.12.2026 — 05.12.2026" или "—"
		private string _bookingDisplay = "—";
		public string BookingDisplay
		{
			get => _bookingDisplay;
			set { _bookingDisplay = value; OnPropertyChanged(); }
		}

		// Полный список броней для всплывающей подсказки
		private string _bookingToolTip;
		public string BookingToolTip
		{
			get => _bookingToolTip;
			set { _bookingToolTip = value; OnPropertyChanged(); }
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string name = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}
	}
}