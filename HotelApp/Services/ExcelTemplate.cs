using System;
using System.Collections.Generic;
using HotelApp.ViewModels;

namespace HotelApp.Services
{
	public static class ExcelTemplate
	{
		// Сохраняет текущие данные гостей в файл Excel в формате импорта.
		public static void SaveCurrentData(string path, IntPtr hotelPtr)
		{
			XlsxFile.Write(path, "Гости", ExcelImportService.Headers, BuildCurrentDataRows(hotelPtr));
		}

		// Формирует строки таблицы на основе текущих записей истории гостей.
		public static List<object[]> BuildCurrentDataRows(IntPtr hotelPtr)
		{
			var guests = GuestsViewModel.LoadSnapshot(hotelPtr);
			var rows = new List<object[]>();
			int number = 0;

			foreach (var guest in guests)
			{
				if (guest.StayHistory == null) continue;
				foreach (var stay in guest.StayHistory)
				{
					number++;
					double pricePerDay = stay.Days > 0 ? stay.Revenue / stay.Days : 0;

					rows.Add(new object[]
					{
						number,
						guest.FullName ?? string.Empty,
						guest.Passport ?? string.Empty,
						stay.BuildingName ?? string.Empty,
						stay.RoomTypeText ?? "Стандарт",
						stay.RoomNumber ?? string.Empty,
						pricePerDay,
						stay.CheckIn.Date,
						stay.CheckOut.Date
					});
				}
			}

			return rows;
		}
	}
}
