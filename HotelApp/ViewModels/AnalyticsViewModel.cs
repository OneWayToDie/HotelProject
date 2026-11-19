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

		public string[] MonthLabels { get; set; }
		public Func<double, string> YFormatterRevenue { get; set; }
		public Func<double, string> YFormatterCount { get; set; }
		public ObservableCollection<RoomTypeDetailsModel> SelectedTypeGuests { get; } = new ObservableCollection<RoomTypeDetailsModel>();
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
			ExpectedRevenue = 0;

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
					// Выручка по текущим заселениям
					if (TryGetStayRevenue(r, out double stayRevenue))
						ExpectedRevenue += stayRevenue;
				}
				else if (r.Status == 2) CleaningRooms++;
			}

			// 2. Бронирования
			DateTime today = DateTime.Today;
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

				// Добавляем в ожидаемую выручку
				ExpectedRevenue += room.BasePrice * days;

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
			MonthLabels = sortedMonths;

			// 4. Формируем данные для графиков
			RevenueSeries = new SeriesCollection
			{
				new ColumnSeries
				{
					Title = "Выручка",
					Values = new ChartValues<double>(sortedMonths.Select(m => revenueByMonth[m])),
					Fill = (Brush)System.Windows.Application.Current.FindResource("PrimaryHueMidBrush")
				}
			};

			OccupancySeries = new SeriesCollection
			{
				new ColumnSeries
				{
					Title = "Кол-во броней",
					Values = new ChartValues<double>(sortedMonths.Select(m => (double)countByMonth[m])),
					Fill = (Brush)System.Windows.Application.Current.FindResource("SecondaryHueMidBrush")
				}
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

			// 7. Общая выручка
			TotalRevenue = NativeMethods.Hotel_GetTotalRevenue(_hotelPtr) + ExpectedRevenue;

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

		private void ShowRoomTypeDetails(string typeName)
		{
			SelectedTypeGuests.Clear();

			// Определяем числовой тип: 0 - Стандарт, 1 - Люкс, 2 - Конференц-зал
			int targetType = 0;
			if (typeName == "Люкс") targetType = 1;
			else if (typeName == "Конференц-зал") targetType = 2;

			// 1. Получаем номера
			var rooms = new CRoom[1000];
			int roomCount = NativeMethods.Hotel_GetAllRooms(_hotelPtr, rooms, 1000);

			var roomTypeDict = new Dictionary<int, int>();
			var roomNumberDict = new Dictionary<int, string>();

			for (int i = 0; i < roomCount; i++)
			{
				roomTypeDict[rooms[i].Id] = rooms[i].Type;
				roomNumberDict[rooms[i].Id] = rooms[i].Number;

				// Текущие проживания
				if (rooms[i].Type == targetType && rooms[i].Status == 1)
				{
					SelectedTypeGuests.Add(new RoomTypeDetailsModel
					{
						GuestName = rooms[i].GuestName,
						Passport = rooms[i].GuestPassport,
						RoomNumber = rooms[i].Number,
						DatesDisplay = $"{rooms[i].CheckInDate} - {rooms[i].CheckOutDate}"
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

					SelectedTypeGuests.Add(new RoomTypeDetailsModel
					{
						GuestName = bookings[i].GuestName,
						Passport = bookings[i].Passport,
						RoomNumber = roomNum,
						DatesDisplay = $"{bookings[i].CheckInDate} - {bookings[i].CheckOutDate}"
					});
				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
		private void OnPropertyChanged([CallerMemberName] string name = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}
	}
}