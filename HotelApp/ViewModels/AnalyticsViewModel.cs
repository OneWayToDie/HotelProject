using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using HotelApp.Models;
using HotelApp.Services;
using LiveCharts;
using LiveCharts.Wpf;

namespace HotelApp.ViewModels
{
	public class AnalyticsViewModel : INotifyPropertyChanged
	{
		private IntPtr _hotelPtr;

		// --- Данные для карточек ---
		public int TotalRooms { get; private set; }
		public int FreeRooms { get; private set; }
		public int OccupiedRooms { get; private set; }
		public int CleaningRooms { get; private set; }
		public int TotalGuests { get; private set; }
		public double ExpectedRevenue { get; private set; }
		public double TotalRevenue { get; private set; }
		public double OccupancyPercent { get; private set; }

		public Brush OccupancyColor
		{
			get
			{
				if (OccupancyPercent >= 80) return Brushes.IndianRed;
				if (OccupancyPercent >= 50) return Brushes.Orange;
				return Brushes.MediumSeaGreen;
			}
		}
		public string OccupancyLabel => $"{OccupancyPercent:F0}%";
		public string RevenueLabel => $"{ExpectedRevenue:N0} ₽";
		public string TotalRevenueLabel => $"{TotalRevenue:N0} ₽";

		// --- Данные для графиков ---
		public SeriesCollection RevenueSeries { get; set; }
		public SeriesCollection OccupancySeries { get; set; }
		public SeriesCollection RoomTypeSeries { get; set; }

		private bool _isLineChart;

		// Тип графиков: столбцы (по умолчанию) или линии.
		public bool IsLineChart
		{
			get => _isLineChart;
			set
			{
				if (_isLineChart == value) return;
				_isLineChart = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(ChartTypeLabel));
				Refresh();
			}
		}

		public string ChartTypeLabel => _isLineChart ? "Линии" : "Столбцы";

		public string[] MonthLabels { get; set; }

		// Числовая ось X: месяц с индексом i стоит на значении i. Полтора шага
		// запаса слева и справа: LiveCharts не подписывает значение вплотную
		// к правой границе, поэтому без запаса последний месяц (апрель) пропадал.
		public double AxisMinValue => -1.5;
		public double AxisMaxValue { get; private set; } = double.NaN;
		public Func<double, string> MonthLabelFormatter { get; private set; }

		public Func<double, string> YFormatterRevenue { get; set; }
		public Func<double, string> YFormatterCount { get; set; }

		// Коллекция заменяется целиком: DataGrid получает одно уведомление
		// вместо Clear + Add на каждый элемент (это и вызывало подвисания).
		private ObservableCollection<RoomTypeDetailsModel> _selectedTypeGuests =
			new ObservableCollection<RoomTypeDetailsModel>();

		public ObservableCollection<RoomTypeDetailsModel> SelectedTypeGuests
		{
			get => _selectedTypeGuests;
			private set
			{
				_selectedTypeGuests = value;
				OnPropertyChanged();
			}
		}

		public ICommand ShowRoomTypeDetailsCommand { get; }
		

		public AnalyticsViewModel(IntPtr hotelPtr)
		{
			_hotelPtr = hotelPtr;

			// Инициализация коллекций для графиков
			RevenueSeries = new SeriesCollection();
			OccupancySeries = new SeriesCollection();
			RoomTypeSeries = new SeriesCollection();
			ShowRoomTypeDetailsCommand = new RelayCommand(param => ShowRoomTypeDetails(param as string));

			MonthLabels = new string[0];

			// Подпись числовой оси: целое значение -> месяц, дробные засечки не подписываем.
			MonthLabelFormatter = value =>
			{
				double rounded = Math.Round(value);
				if (Math.Abs(value - rounded) > 0.25) return string.Empty;
				int index = (int)rounded;
				if (index < 0 || index >= MonthLabels.Length) return string.Empty;
				return MonthLabels[index];
			};

			YFormatterRevenue = value => value.ToString("N0") + " ₽";
			YFormatterCount = value => value.ToString("N0");

			Refresh();
		}

		public void Refresh()
		{
			try { Recalculate(); }
			catch (Exception ex) { AppDialog.Error("Не удалось рассчитать аналитику.\n" + ex.Message, "Аналитика"); }
		}

		private void Recalculate()
		{
			// Сбрасываем счетчики
			FreeRooms = 0;
			OccupiedRooms = 0;
			CleaningRooms = 0;

			// Полная ожидаемая выручка (текущие заселения + активные/будущие брони)
			// используется для расчёта общей выручки за всё время.
			double allExpectedRevenue = 0;
			// Выручка за текущий месяц от всех заселённых гостей — показывается в карточке.
			double monthlyCheckedInRevenue = 0;

			DateTime today = DateTime.Today;
			DateTime monthStart = new DateTime(today.Year, today.Month, 1);
			DateTime monthEnd = monthStart.AddMonths(1);

			// 1. Номера
			var rooms = new CRoom[1000];
			int count = NativeMethods.Hotel_GetAllRooms(_hotelPtr, rooms, 1000);
			TotalRooms = count;

			var roomsById = new Dictionary<int, CRoom>();
			for (int i = 0; i < count; i++)
			{
				var r = rooms[i];
				roomsById[r.Id] = r;
				if (r.Status == 0) FreeRooms++;
				else if (r.Status == 1)
				{
					OccupiedRooms++;
					// Выручка по текущим заселениям (для общей суммы за всё время)
					if (TryGetStayRevenue(r, out double stayRevenue))
						allExpectedRevenue += stayRevenue;
					// Часть проживания, приходящаяся на текущий месяц
					if (TryGetMonthRevenue(r, monthStart, monthEnd, out double monthRevenue))
						monthlyCheckedInRevenue += monthRevenue;
				}
				else if (r.Status == 2) CleaningRooms++;
			}

			// 2. Бронирования
			var bookings = new CBooking[1000];
			int bookingCount = NativeMethods.Hotel_GetAllBookings(_hotelPtr, bookings, 1000);

			// Словари для графиков
			var revenueByMonth = new Dictionary<string, double>();
			var countByMonth = new Dictionary<string, int>();
			var typeCounts = new Dictionary<string, int> { { "Стандарт", 0 }, { "Люкс", 0 }, { "Конференц-зал", 0 } };

			for (int i = 0; i < bookingCount; i++)
			{
				var booking = bookings[i];
				if (!roomsById.TryGetValue(booking.RoomId, out var room)) continue;
				if (!DateTime.TryParse(booking.CheckInDate, out var bookingIn) ||
					!DateTime.TryParse(booking.CheckOutDate, out var bookingOut))
					continue;

				// Проживание уже закончилось — деньги по нему не ожидаются
				if (bookingOut.Date <= today) continue;

				int days = (bookingOut.Date - bookingIn.Date).Days;
				if (days <= 0) continue;

				// Гость уже заселен по этой брони: его проживание посчитано выше,
				// повторно ту же сумму не добавляем.
				if (room.Status == 1 &&
					DateTime.TryParse(room.CheckInDate, out var stayIn) &&
					DateTime.TryParse(room.CheckOutDate, out var stayOut) &&
					bookingIn.Date < stayOut.Date && stayIn.Date < bookingOut.Date)
					continue;

				// Добавляем в полную ожидаемую выручку (для общей суммы за всё время)
				allExpectedRevenue += room.BasePrice * days;

				// Данные для графиков
				string monthLabel = bookingIn.ToString("MMM yyyy", new CultureInfo("ru-RU"));
				double revenue = room.BasePrice * days;

				if (!revenueByMonth.ContainsKey(monthLabel))
				{
					revenueByMonth[monthLabel] = 0;
					countByMonth[monthLabel] = 0;
				}

				revenueByMonth[monthLabel] += revenue;
				countByMonth[monthLabel] += 1;

				// Подсчет типов номеров
				string typeName = room.Type == 1 ? "Люкс" : (room.Type == 2 ? "Конференц-зал" : "Стандарт");
				if (typeCounts.ContainsKey(typeName)) typeCounts[typeName]++;
			}

			// 3. Сортируем месяцы по дате
			var sortedMonths = revenueByMonth.Keys
				.OrderBy(k => DateTime.ParseExact("01 " + k, "dd MMM yyyy", new CultureInfo("ru-RU")))
				.ToArray();

			// 4. Формируем данные для графиков
			var primaryBrush = (Brush)System.Windows.Application.Current.FindResource("PrimaryHueMidBrush");
			var secondaryBrush = (Brush)System.Windows.Application.Current.FindResource("SecondaryHueMidBrush");

			MonthLabels = sortedMonths;

			// Диапазон оси один и тот же для столбцов и линий — при переключении
			// график не смещается, а крайние месяцы остаются внутри области.
			AxisMaxValue = sortedMonths.Length > 0
				? sortedMonths.Length + 0.5
				: double.NaN;

			RevenueSeries = new SeriesCollection
			{
				BuildSeries("Выручка",
					sortedMonths.Select(m => revenueByMonth[m]), primaryBrush)
			};

			OccupancySeries = new SeriesCollection
			{
				BuildSeries("Кол-во броней",
					sortedMonths.Select(m => (double)countByMonth[m]), secondaryBrush)
			};

			RoomTypeSeries = new SeriesCollection
			{
				new PieSeries { Title = "Стандарт", Values = new ChartValues<double> { typeCounts["Стандарт"] }, DataLabels = true },
				new PieSeries { Title = "Люкс", Values = new ChartValues<double> { typeCounts["Люкс"] }, DataLabels = true },
				new PieSeries { Title = "Конференц-зал", Values = new ChartValues<double> { typeCounts["Конференц-зал"] }, DataLabels = true }
			};

			// 5. Гости
			var guests = new CGuest[1000];
			TotalGuests = NativeMethods.Hotel_GetAllGuests(_hotelPtr, guests, 1000);

			// 6. Процент загрузки
			OccupancyPercent = TotalRooms > 0 ? (OccupiedRooms * 100.0 / TotalRooms) : 0;

			// Карточка: выручка за текущий месяц от всех заселённых гостей
			ExpectedRevenue = monthlyCheckedInRevenue;

			// 7. Общая выручка за всё время: накопленная + полная ожидаемая
			TotalRevenue = NativeMethods.Hotel_GetTotalRevenue(_hotelPtr) + allExpectedRevenue;

			OnPropertyChanged(string.Empty);
		}

		private static bool TryGetStayRevenue(CRoom room, out double revenue)
		{
			revenue = 0;
			if (!DateTime.TryParse(room.CheckInDate, out var dateIn) ||
				!DateTime.TryParse(room.CheckOutDate, out var dateOut))
				return false;
			int days = (dateOut.Date - dateIn.Date).Days;
			if (days <= 0) return false;
			revenue = room.BasePrice * days;
			return true;
		}

		// Выручка заселённого гостя, приходящаяся на указанный период (месяц).
		private static bool TryGetMonthRevenue(CRoom room, DateTime periodStart, DateTime periodEnd, out double revenue)
		{
			revenue = 0;
			if (!DateTime.TryParse(room.CheckInDate, out var dateIn) ||
				!DateTime.TryParse(room.CheckOutDate, out var dateOut))
				return false;

			DateTime from = dateIn.Date > periodStart ? dateIn.Date : periodStart;
			DateTime to = dateOut.Date < periodEnd ? dateOut.Date : periodEnd;
			int days = (to - from).Days;
			if (days <= 0) return false;

			revenue = room.BasePrice * days;
			return true;
		}

		// Создаёт серию нужного типа: столбцы или линию.
		private Series BuildSeries(string title, IEnumerable<double> values, Brush color)
		{
			var chartValues = new ChartValues<double>(values);

			if (!_isLineChart)
			{
				return new ColumnSeries
				{
					Title = title,
					Values = chartValues,
					Fill = color
				};
			}

			return new LineSeries
			{
				Title = title,
				Values = chartValues,
				Stroke = color,
				Fill = Brushes.Transparent,
				PointGeometrySize = 9,
				StrokeThickness = 2.5,
				LineSmoothness = 0.3
			};
		}

		// Топ гостей по сумме (проживания + брони). Используется экспортом отчёта.
		public List<GuestModel> GetTopGuests(int count)
		{
			if (count <= 0) return new List<GuestModel>();

			return GuestsViewModel.LoadSnapshot(_hotelPtr)
				.Where(g => g.TotalRevenue > 0)
				.OrderByDescending(g => g.TotalRevenue)
				.ThenByDescending(g => g.TotalDays)
				.Take(count)
				.ToList();
		}

		private void ShowRoomTypeDetails(string typeName)
		{
			// Определяем числовой тип: 0 - Стандарт, 1 - Люкс, 2 - Конференц-зал
			int targetType = 0;
			if (typeName == "Люкс") targetType = 1;
			else if (typeName == "Конференц-зал") targetType = 2;

			// 1. Получаем номера
			var rooms = new CRoom[1000];
			int roomCount = NativeMethods.Hotel_GetAllRooms(_hotelPtr, rooms, 1000);

			var roomTypeDict = new Dictionary<int, int>(roomCount);
			var roomNumberDict = new Dictionary<int, string>(roomCount);

			// Сразу оцениваем размер результата, чтобы не перевыделять память.
			var items = new List<RoomTypeDetailsModel>(roomCount);

			for (int i = 0; i < roomCount; i++)
			{
				roomTypeDict[rooms[i].Id] = rooms[i].Type;
				roomNumberDict[rooms[i].Id] = rooms[i].Number;

				// Текущие проживания
				if (rooms[i].Type == targetType && rooms[i].Status == 1)
				{
					items.Add(new RoomTypeDetailsModel
					{
						GuestName = rooms[i].GuestName,
						Passport = rooms[i].GuestPassport,
						RoomNumber = rooms[i].Number,
						DatesDisplay = rooms[i].CheckInDate + " - " + rooms[i].CheckOutDate
					});
				}
			}

			// 2. Получаем бронирования
			var bookings = new CBooking[1000];
			int bookingCount = NativeMethods.Hotel_GetAllBookings(_hotelPtr, bookings, 1000);

			for (int i = 0; i < bookingCount; i++)
			{
				if (roomTypeDict.TryGetValue(bookings[i].RoomId, out int rType) && rType == targetType)
				{
					string roomNum = roomNumberDict.TryGetValue(bookings[i].RoomId, out string num) ? num : "?";

					items.Add(new RoomTypeDetailsModel
					{
						GuestName = bookings[i].GuestName,
						Passport = bookings[i].Passport,
						RoomNumber = roomNum,
						DatesDisplay = bookings[i].CheckInDate + " - " + bookings[i].CheckOutDate
					});
				}
			}

			// Одна операция вместо Clear + Add на каждый элемент: таблица не дёргается.
			SelectedTypeGuests = new ObservableCollection<RoomTypeDetailsModel>(items);
		}

		public event PropertyChangedEventHandler PropertyChanged;
		private void OnPropertyChanged([CallerMemberName] string name = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}
	}
}