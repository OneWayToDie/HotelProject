using System;
using System.Collections.Generic;
using System.Globalization;
using HotelApp.ViewModels;

namespace HotelApp.Services
{
	// Формирует аналитический отчёт в Excel: сводка по отелю и топ-5 гостей по сумме.
	public static class AnalyticsReportService
	{
		public const int TopGuestsCount = 5;

		public static void Save(string path, AnalyticsViewModel source)
		{
			if (string.IsNullOrWhiteSpace(path))
				throw new ArgumentException("Не указан путь для сохранения отчёта.", nameof(path));
			if (source == null)
				throw new ArgumentNullException(nameof(source));

			var rows = new List<object[]>();

			// --- Сводка по отелю ---
			rows.Add(new object[] { "Дата отчёта", DateTime.Today.ToString("dd.MM.yyyy"), null, null, null });
			rows.Add(new object[] { "Всего номеров", source.TotalRooms, null, null, null });
			rows.Add(new object[] { "Свободно номеров", source.FreeRooms, null, null, null });
			rows.Add(new object[] { "Занято номеров", source.OccupiedRooms, null, null, null });
			rows.Add(new object[] { "На уборке", source.CleaningRooms, null, null, null });
			rows.Add(new object[] { "Загрузка отеля, %", Math.Round(source.OccupancyPercent, 1), null, null, null });
			rows.Add(new object[] { "Ожидаемая выручка, ₽", Math.Round(source.ExpectedRevenue, 2), null, null, null });
			rows.Add(new object[] { "Общая выручка, ₽", Math.Round(source.TotalRevenue, 2), null, null, null });

			// --- Топ гостей ---
			rows.Add(new object[] { string.Empty, null, null, null, null });
			rows.Add(new object[] { "ТОП-" + TopGuestsCount + " ГОСТЕЙ ПО СУММЕ", null, null, null, null });
			rows.Add(new object[] { "Место", "ФИО гостя", "Паспорт", "Суток", "Сумма, ₽" });

			var topGuests = source.GetTopGuests(TopGuestsCount);
			if (topGuests.Count == 0)
			{
				rows.Add(new object[] { "Нет данных о гостях", null, null, null, null });
			}
			else
			{
				for (int i = 0; i < topGuests.Count; i++)
				{
					var guest = topGuests[i];
					rows.Add(new object[]
					{
						i + 1,
						guest.FullName ?? "—",
						guest.Passport ?? string.Empty,
						guest.TotalDays,
						Math.Round(guest.TotalRevenue, 2)
					});
				}
			}

			XlsxFile.Write(
				path,
				"Аналитика отеля",
				new[] { "Показатель", "Значение", string.Empty, string.Empty, string.Empty },
				rows,
				new[] { 30, 34, 18, 10, 16 },
				includeRoomTypeValidation: false,
				includeAutoFilter: false);
		}

		// Сумма в формате «1 234 567 ₽» — для сообщений и подсказок.
		public static string FormatMoney(double value)
		{
			return value.ToString("N0", CultureInfo.GetCultureInfo("ru-RU")) + " ₽";
		}
	}
}
