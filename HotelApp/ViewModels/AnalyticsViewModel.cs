using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using HotelApp.Services;

namespace HotelApp.ViewModels
{
	public class AnalyticsViewModel : INotifyPropertyChanged
	{
		private IntPtr _hotelPtr;

		// Данные для карточек
		public int TotalRooms { get; private set; }
		public int FreeRooms { get; private set; }
		public int OccupiedRooms { get; private set; }
		public int CleaningRooms { get; private set; }
		public int TotalGuests { get; private set; }
		public double ExpectedRevenue { get; private set; }
		public double TotalRevenue { get; private set; }
		public double OccupancyPercent { get; private set; }

		// Для цветного прогресс-бара
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

		public AnalyticsViewModel(IntPtr hotelPtr)
		{
			_hotelPtr = hotelPtr;
			Refresh();
		}

		public void Refresh()
		{
			try
			{
				Recalculate();
			}
			catch (Exception ex)
			{
				AppDialog.Error("Не удалось рассчитать аналитику.\n" + ex.Message, "Аналитика");
			}
		}

		private void Recalculate()
		{
			// Сбрасываем счетчики, чтобы они не накапливались при повторном пересчете
			FreeRooms = 0;
			OccupiedRooms = 0;
			CleaningRooms = 0;
			ExpectedRevenue = 0;

			// 1. Номера
			var rooms = new CRoom[1000];
			int count = NativeMethods.Hotel_GetAllRooms(_hotelPtr, rooms, 1000);
			TotalRooms = count;

			// Нужно для расчета цены броней (цена хранится в номере)
			var roomsById = new Dictionary<int, CRoom>();

			for (int i = 0; i < count; i++)
			{
				var r = rooms[i];
				roomsById[r.Id] = r;

				if (r.Status == 0) FreeRooms++;
				else if (r.Status == 1)
				{
					OccupiedRooms++;

					// Считаем выручку по текущим заселениям (цена за сутки x число суток)
					if (TryGetStayRevenue(r, out double stayRevenue))
						ExpectedRevenue += stayRevenue;
				}
				else if (r.Status == 2) CleaningRooms++;
			}

			// 2. Бронирования (их создает в том числе импорт Excel)
			// Берем только незавершенные брони: они еще принесут деньги.
			DateTime today = DateTime.Today;
			var bookings = new CBooking[1000];
			int bookingCount = NativeMethods.Hotel_GetAllBookings(_hotelPtr, bookings, 1000);

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

				ExpectedRevenue += room.BasePrice * days;
			}

			// 3. Гости
			var guests = new CGuest[1000];
			TotalGuests = NativeMethods.Hotel_GetAllGuests(_hotelPtr, guests, 1000);

			// 4. Процент загрузки
			OccupancyPercent = TotalRooms > 0 ? (OccupiedRooms * 100.0 / TotalRooms) : 0;

			// 5. Общая выручка отеля:
			//    накопленная в C++ при выселении + ожидаемая от заселений и броней.
			TotalRevenue = NativeMethods.Hotel_GetTotalRevenue(_hotelPtr) + ExpectedRevenue;

			// Обновляем все привязки: карточки должны меняться сразу после импорта,
			// а не только при повторном открытии вкладки.
			OnPropertyChanged(string.Empty);
		}

		// Выручка по текущему заселению: цена за сутки x число суток проживания.
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

		public event PropertyChangedEventHandler PropertyChanged;

		private void OnPropertyChanged([CallerMemberName] string name = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}
	}
}