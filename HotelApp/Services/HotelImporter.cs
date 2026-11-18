using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HotelApp.Models;
using static HotelApp.Services.NativeMethods;

namespace HotelApp.Services
{
	public class ImportSummary
	{
		public int TotalRows { get; set; }
		public int ImportedRows { get; set; }
		public int FailedRows { get; set; }
		public int CreatedBuildings { get; set; }
		public int CreatedRooms { get; set; }
	}

	// Проверка и применение импортированных строк к нативной модели отеля.
	public static class HotelImporter
	{
		private static readonly CultureInfo RuCulture = CultureInfo.GetCultureInfo("ru-RU");

		public static void Analyze(IEnumerable<ImportRow> rows, IntPtr hotelPtr)
		{
			var buildings = ReadBuildings(hotelPtr);
			var rooms = ReadRooms(hotelPtr);
			var bookings = ReadBookings(hotelPtr);

			// Карта: id номера -> "корпус|номер"
			var roomKeyById = new Dictionary<int, string>();
			foreach (var room in rooms)
			{
				var building = buildings.FirstOrDefault(b => b.Id == room.BuildingId);
				if (building.Id == 0) continue;
				roomKeyById[room.Id] = MakeKey(building.Name, room.Number);
			}

			// Занятые интервалы по каждому номеру: существующие брони
			var intervals = new Dictionary<string, List<Tuple<string, string>>>(StringComparer.OrdinalIgnoreCase);
			foreach (var booking in bookings)
			{
				if (!roomKeyById.TryGetValue(booking.RoomId, out string key)) continue;
				if (string.IsNullOrWhiteSpace(booking.CheckInDate) || string.IsNullOrWhiteSpace(booking.CheckOutDate)) continue;
				AddInterval(intervals, key, booking.CheckInDate, booking.CheckOutDate);
			}

			foreach (var row in rows)
			{
				row.Error = string.Empty;

				// 1. Обязательные поля
				var missing = new List<string>();
				if (string.IsNullOrWhiteSpace(row.FullName)) missing.Add("ФИО");
				if (string.IsNullOrWhiteSpace(row.Passport)) missing.Add("Паспорт");
				if (string.IsNullOrWhiteSpace(row.Building)) missing.Add("Корпус");
				if (string.IsNullOrWhiteSpace(row.RoomType)) missing.Add("Тип номера");
				if (string.IsNullOrWhiteSpace(row.RoomNumber)) missing.Add("Номер");
				if (string.IsNullOrWhiteSpace(row.PriceText)) missing.Add("Цена за сутки");
				if (string.IsNullOrWhiteSpace(row.CheckInText)) missing.Add("Дата заезда");
				if (string.IsNullOrWhiteSpace(row.CheckOutText)) missing.Add("Дата выезда");

				if (missing.Count > 0)
				{
					row.Error = "Не заполнено: " + string.Join(", ", missing);
					continue;
				}

				// 2. ФИО: только буквы, пробел и дефис
				string fullName = InputRules.NormalizeFullName(row.FullName);
				if (!InputRules.IsValidFullName(fullName))
				{
					row.Error = "ФИО: только буквы, пробел и дефис (до 100 символов).";
					continue;
				}

				// 3. Паспорт: строго новый формат «1234 567890»
				if (!InputRules.TryNormalizePassport(row.Passport, out string passport))
				{
					row.Error = "Паспорт: ожидается формат 1234 567890 (4 цифры, пробел, 6 цифр).";
					continue;
				}

				// 4. Название корпуса: любая строка до 50 символов
				string buildingName = (row.Building ?? string.Empty).Trim();
				if (!InputRules.IsValidBuildingName(buildingName))
				{
					row.Error = "Название корпуса: от 1 до 50 символов.";
					continue;
				}

				// 5. Номер комнаты: только цифры
				string roomNumber = NormalizeIntegerCell(row.RoomNumber);
				if (!InputRules.IsValidRoomNumber(roomNumber))
				{
					row.Error = "Номер комнаты: только цифры (до 5 знаков).";
					continue;
				}

				// 6. Цена: только целые рубли
				if (!TryParsePrice(row.PriceText, out double price) || price <= 0)
				{
					row.Error = "Цена: целое число рублей больше нуля.";
					continue;
				}
				row.Price = price;

				// 7. Тип номера
				if (!TryParseRoomType(row.RoomType, out int roomType))
				{
					row.Error = "Тип номера: допустимо «Стандарт», «Люкс» или «Конференц-зал».";
					continue;
				}
				row.RoomTypeValue = roomType;

				// 8. Даты
				if (!TryParseDate(row.CheckInText, out DateTime checkIn))
				{
					row.Error = "Неверная дата заезда (ожидается дд.мм.гггг).";
					continue;
				}
				if (!TryParseDate(row.CheckOutText, out DateTime checkOut))
				{
					row.Error = "Неверная дата выезда (ожидается дд.мм.гггг).";
					continue;
				}
				if (checkOut <= checkIn)
				{
					row.Error = "Дата выезда должна быть позже даты заезда.";
					continue;
				}
				row.CheckIn = checkIn;
				row.CheckOut = checkOut;

				// 9. Пересечение с существующими бронями и с другими строками файла
				string key = MakeKey(buildingName, roomNumber);
				string inIso = checkIn.ToString("yyyy-MM-dd");
				string outIso = checkOut.ToString("yyyy-MM-dd");

				if (HasOverlap(intervals, key, inIso, outIso))
				{
					row.Error = "Даты пересекаются с другой бронью этого номера.";
					continue;
				}

				AddInterval(intervals, key, inIso, outIso);

				// Приводим значения к единому виду — их же использует Apply
				row.FullName = fullName;
				row.Passport = passport;
				row.Building = buildingName;
				row.RoomNumber = roomNumber;
			}
		}

		public static ImportSummary Apply(IEnumerable<ImportRow> rows, IntPtr hotelPtr)
		{
			var summary = new ImportSummary();

			foreach (var row in rows)
			{
				summary.TotalRows++;

				// Строки с ошибками не импортируем никогда
				if (row.HasError)
				{
					summary.FailedRows++;
					continue;
				}

				try
				{
					ImportRow(row, hotelPtr, summary);
				}
				catch (Exception ex)
				{
					row.Error = "Ошибка импорта строки: " + ex.Message;
					summary.FailedRows++;
				}
			}

			return summary;
		}

		private static void ImportRow(ImportRow row, IntPtr hotelPtr, ImportSummary summary)
		{
			var buildings = ReadBuildings(hotelPtr);
			var building = buildings.FirstOrDefault(b =>
				string.Equals((b.Name ?? string.Empty).Trim(), (row.Building ?? string.Empty).Trim(),
					StringComparison.OrdinalIgnoreCase));

			if (building.Id == 0)
			{
				NativeMethods.Hotel_AddBuilding(hotelPtr, row.Building.Trim());
				summary.CreatedBuildings++;

				buildings = ReadBuildings(hotelPtr);
				building = buildings.FirstOrDefault(b =>
					string.Equals((b.Name ?? string.Empty).Trim(), (row.Building ?? string.Empty).Trim(),
						StringComparison.OrdinalIgnoreCase));
			}

			if (building.Id == 0)
			{
				row.Error = "Не удалось создать корпус.";
				summary.FailedRows++;
				return;
			}

			var rooms = ReadRooms(hotelPtr);
			var room = rooms.FirstOrDefault(r =>
				r.BuildingId == building.Id &&
				string.Equals((r.Number ?? string.Empty).Trim(), (row.RoomNumber ?? string.Empty).Trim(),
					StringComparison.OrdinalIgnoreCase));

			if (room.Id == 0)
			{
				NativeMethods.Hotel_AddRoom(hotelPtr, building.Id, row.RoomTypeValue, row.RoomNumber.Trim(), row.Price);
				summary.CreatedRooms++;

				rooms = ReadRooms(hotelPtr);
				room = rooms.FirstOrDefault(r =>
					r.BuildingId == building.Id &&
					string.Equals((r.Number ?? string.Empty).Trim(), (row.RoomNumber ?? string.Empty).Trim(),
						StringComparison.OrdinalIgnoreCase));
			}

			if (room.Id == 0)
			{
				row.Error = "Не удалось создать номер.";
				summary.FailedRows++;
				return;
			}

			bool booked = NativeMethods.Hotel_BookRoom(
				hotelPtr,
				room.Id,
				row.FullName.Trim(),
				row.Passport.Trim(),
				row.CheckIn.ToString("yyyy-MM-dd"),
				row.CheckOut.ToString("yyyy-MM-dd"));

			if (booked)
			{
				summary.ImportedRows++;
			}
			else
			{
				row.Error = "Бронирование не создано: даты пересекаются с существующей бронью.";
				summary.FailedRows++;
			}
		}

		#region Проверки

		public static bool TryParseDate(string value, out DateTime result)
		{
			result = DateTime.MinValue;
			if (string.IsNullOrWhiteSpace(value)) return false;

			string text = value.Trim();
			string[] formats =
			{
				"dd.MM.yyyy", "d.M.yyyy", "dd.MM.yy", "d.M.yy",
				"dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd", "dd-MM-yyyy", "d-M-yyyy"
			};

			if (DateTime.TryParseExact(text, formats, CultureInfo.InvariantCulture,
				DateTimeStyles.None, out result))
				return true;

			return DateTime.TryParse(text, RuCulture, DateTimeStyles.None, out result);
		}

		public static bool TryParsePrice(string value, out double result)
		{
			result = 0;
			if (string.IsNullOrWhiteSpace(value)) return false;

			string text = value.Trim().ToLowerInvariant()
				.Replace('\u00A0', ' ')
				.Replace("₽", string.Empty)
				.Replace("руб.", string.Empty)
				.Replace("руб", string.Empty)
				.Replace("р.", string.Empty)
				.Replace(" ", string.Empty);

			// Цена — только целые рубли
			text = NormalizeIntegerCell(text);
			if (!InputRules.IsValidPrice(text, out int price)) return false;

			result = price;
			return true;
		}

		// Excel иногда отдаёт числовые ячейки как "101.0" — приводим к целому виду.
		private static string NormalizeIntegerCell(string value)
		{
			string text = (value ?? string.Empty).Trim();
			if (text.Length == 0 || !text.Contains(".")) return text;

			if (decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal number) &&
				number == decimal.Truncate(number))
				return ((long)number).ToString(CultureInfo.InvariantCulture);

			return text;
		}

		public static bool TryParseRoomType(string value, out int roomType)
		{
			roomType = 0;
			if (string.IsNullOrWhiteSpace(value)) return false;

			string text = value.Trim().ToLowerInvariant().Replace('ё', 'е');

			if (text == "стандарт" || text == "standard")
			{
				roomType = 0;
				return true;
			}
			if (text == "люкс" || text == "lux")
			{
				roomType = 1;
				return true;
			}
			if (text == "конференц-зал" || text == "конференц зал" ||
				text == "конференц" || text == "conference")
			{
				roomType = 2;
				return true;
			}

			return false;
		}

		#endregion

		#region Вспомогательное

		private static string MakeKey(string building, string roomNumber)
		{
			return (building ?? string.Empty).Trim().ToLowerInvariant().Replace('ё', 'е') + "|" +
				   (roomNumber ?? string.Empty).Trim().ToLowerInvariant();
		}

		private static void AddInterval(
			Dictionary<string, List<Tuple<string, string>>> intervals,
			string key, string inIso, string outIso)
		{
			if (!intervals.TryGetValue(key, out var list))
			{
				list = new List<Tuple<string, string>>();
				intervals[key] = list;
			}
			list.Add(Tuple.Create(inIso, outIso));
		}

		private static bool HasOverlap(
			Dictionary<string, List<Tuple<string, string>>> intervals,
			string key, string inIso, string outIso)
		{
			if (!intervals.TryGetValue(key, out var list)) return false;

			foreach (var interval in list)
			{
				// Пересечение [in, out) и [existingIn, existingOut)
				if (string.CompareOrdinal(inIso, interval.Item2) < 0 &&
					string.CompareOrdinal(interval.Item1, outIso) < 0)
					return true;
			}

			return false;
		}

		private static List<CBuilding> ReadBuildings(IntPtr hotelPtr)
		{
			var buffer = new CBuilding[1000];
			int count = NativeMethods.Hotel_GetAllBuildings(hotelPtr, buffer, 1000);

			var list = new List<CBuilding>(count);
			for (int i = 0; i < count; i++) list.Add(buffer[i]);
			return list;
		}

		private static List<CRoom> ReadRooms(IntPtr hotelPtr)
		{
			var buffer = new CRoom[2000];
			int count = NativeMethods.Hotel_GetAllRooms(hotelPtr, buffer, 2000);

			var list = new List<CRoom>(count);
			for (int i = 0; i < count; i++) list.Add(buffer[i]);
			return list;
		}

		private static List<CBooking> ReadBookings(IntPtr hotelPtr)
		{
			var buffer = new CBooking[2000];
			int count = NativeMethods.Hotel_GetAllBookings(hotelPtr, buffer, 2000);

			var list = new List<CBooking>(count);
			for (int i = 0; i < count; i++) list.Add(buffer[i]);
			return list;
		}

		#endregion
	}
}
