using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using HotelApp.Models;

namespace HotelApp.Services
{
	// Чтение Excel-файла с гостями: заголовки ищутся по названиям,
	// поэтому порядок колонок в файле не важен.
	public static class ExcelImportService
	{
		public static readonly string[] Headers =
		{
			"№", "ФИО", "Паспорт", "Корпус", "Тип номера",
			"Номер", "Цена за сутки", "Дата заезда", "Дата выезда"
		};

		public static List<ImportRow> Read(string path)
		{
			var sheet = XlsxFile.ReadFirstSheet(path);
			if (sheet == null || sheet.Count == 0)
				throw new InvalidDataException("Файл Excel пуст.");

			var headers = sheet[0].Select(NormalizeHeader).ToList();

			int cName = IndexOf(headers, "фио", "фио гостя", "гость", "ф.и.о", "ф.и.о.");
			int cPassport = IndexOf(headers, "паспорт", "паспортные данные", "серия и номер паспорта");
			int cBuilding = IndexOf(headers, "корпус", "корпус отеля", "здание");
			int cType = IndexOf(headers, "тип номера", "тип");
			int cNumber = IndexOf(headers, "номер", "номер комнаты", "номер номера");
			int cPrice = IndexOf(headers, "цена за сутки", "цена", "стоимость", "цена за сутки, руб", "цена, руб");
			int cIn = IndexOf(headers, "дата заезда", "заезд", "дата заселения");
			int cOut = IndexOf(headers, "дата выезда", "выезд", "дата выселения");

			var missing = new List<string>();
			if (cName < 0) missing.Add("ФИО");
			if (cPassport < 0) missing.Add("Паспорт");
			if (cBuilding < 0) missing.Add("Корпус");
			if (cType < 0) missing.Add("Тип номера");
			if (cNumber < 0) missing.Add("Номер");
			if (cPrice < 0) missing.Add("Цена за сутки");
			if (cIn < 0) missing.Add("Дата заезда");
			if (cOut < 0) missing.Add("Дата выезда");

			if (missing.Count > 0)
				throw new InvalidDataException(
					"В файле не найдены обязательные колонки: " + string.Join(", ", missing) + ".");

			var rows = new List<ImportRow>();
			for (int i = 1; i < sheet.Count; i++)
			{
				var cells = sheet[i];

				var row = new ImportRow
				{
					LineNumber = i + 1,
					FullName = Get(cells, cName),
					Passport = Get(cells, cPassport),
					Building = Get(cells, cBuilding),
					RoomType = Get(cells, cType),
					RoomNumber = Get(cells, cNumber),
					PriceText = Get(cells, cPrice),
					CheckInText = Get(cells, cIn),
					CheckOutText = Get(cells, cOut)
				};

				bool empty = string.IsNullOrWhiteSpace(row.FullName)
					&& string.IsNullOrWhiteSpace(row.Passport)
					&& string.IsNullOrWhiteSpace(row.Building)
					&& string.IsNullOrWhiteSpace(row.RoomType)
					&& string.IsNullOrWhiteSpace(row.RoomNumber)
					&& string.IsNullOrWhiteSpace(row.PriceText)
					&& string.IsNullOrWhiteSpace(row.CheckInText)
					&& string.IsNullOrWhiteSpace(row.CheckOutText);

				if (empty) continue;
				rows.Add(row);
			}

			return rows;
		}

		private static string Get(string[] cells, int index)
		{
			if (cells == null || index < 0 || index >= cells.Length) return string.Empty;
			return (cells[index] ?? string.Empty).Trim();
		}

		private static int IndexOf(List<string> headers, params string[] names)
		{
			foreach (var name in names)
			{
				int index = headers.IndexOf(name);
				if (index >= 0) return index;
			}
			return -1;
		}

		private static string NormalizeHeader(string value)
		{
			if (string.IsNullOrWhiteSpace(value)) return string.Empty;

			string result = value.Trim().ToLowerInvariant()
				.Replace('ё', 'е')
				.Replace('\u00A0', ' ');

			// Отрезаем уточнения в скобках: "Цена за сутки (₽)" -> "Цена за сутки"
			result = Regex.Replace(result, @"\([^)]*\)", " ");

			// Убираем двоеточия, точки в конце и лишние пробелы
			result = result.TrimEnd(':', '.', ' ');
			result = Regex.Replace(result, @"\s+", " ");

			return result.Trim();
		}
	}
}
