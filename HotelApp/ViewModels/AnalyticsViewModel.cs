using System;
using System.Linq;
using System.Windows.Media;
using HotelApp.Services;

namespace HotelApp.ViewModels
{
	public class AnalyticsViewModel
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
			// Сбрасываем счетчики, чтобы они не накапливались при повторном пересчете
			FreeRooms = 0;
			OccupiedRooms = 0;
			CleaningRooms = 0;
			ExpectedRevenue = 0;

			// 1. Номера
			var rooms = new CRoom[1000];
			int count = NativeMethods.Hotel_GetAllRooms(_hotelPtr, rooms, 1000);
			TotalRooms = count;

			for (int i = 0; i < count; i++)
			{
				var r = rooms[i];
				if (r.Status == 0) FreeRooms++;
				else if (r.Status == 1)
				{
					OccupiedRooms++;

					// Считаем выручку по занятым номерам (по датам)
					if (DateTime.TryParse(r.CheckInDate, out var dateIn) &&
						DateTime.TryParse(r.CheckOutDate, out var dateOut))
					{
						int days = (dateOut - dateIn).Days;
						if (days > 0) ExpectedRevenue += r.BasePrice * days;
					}
				}
				else if (r.Status == 2) CleaningRooms++;
			}

			// 2. Гости
			var guests = new CGuest[1000];
			TotalGuests = NativeMethods.Hotel_GetAllGuests(_hotelPtr, guests, 1000);

			// 3. Процент загрузки
			OccupancyPercent = TotalRooms > 0 ? (OccupiedRooms * 100.0 / TotalRooms) : 0;

			// 4. Общая выручка отеля (накопленная в C++ при выселении)
			TotalRevenue = NativeMethods.Hotel_GetTotalRevenue(_hotelPtr);
		}
	}
}