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
		// Получает или задаёт общее количество строк импорта.
		public int TotalRows { get; set; }

		// Получает или задаёт количество успешно импортированных строк.
		public int ImportedRows { get; set; }

		// Получает или задаёт количество строк с ошибками.
		public int FailedRows { get; set; }

		// Получает или задаёт количество созданных корпусов.
		public int CreatedBuildings { get; set; }

		// Получает или задаёт количество созданных номеров.
		public int CreatedRooms { get; set; }
	}

	public static class HotelImporter
	{
		private static readonly CultureInfo RuCulture = CultureInfo.GetCultureInfo("ru-RU");

		// Выполняет проверку строк импорта и заполняет сообщения об ошибках.
		public static void Analyze(IEnumerable<ImportRow> rows, IntPtr hotelPtr)
		{
			var buildings = ReadBuildings(hotelPtr);
			var rooms = ReadRooms(hotelPtr);
			var bookings = ReadBookings(hotelPtr);

			var roomKeyById = new Dictionary<int, string>();
			foreach (var room in rooms)
			{
				var buildingName = buildings.ContainsKey(room.BuildingId) ? buildings[room.BuildingId] : string.Empty;
				roomKeyById[room.Id] = RoomKey(buildingName, room.Number);
			}

			var bookingsByRoom = new Dictionary<string, List<(DateTime In, DateTime Out)>>();
			foreach (var booking in bookings)
			{
				if (!roomKeyById.TryGetValue(booking.RoomId, out var key)) continue;
				if (!TryNormalizeDates(booking.CheckInDate, booking.CheckOutDate, out var inDate, out var outDate, out _))
					continue;

				if (!bookingsByRoom.TryGetValue(key, out var list))
				{
					list = new List<(DateTime In, DateTime Out)>();
					bookingsByRoom[key] = list;
				}
				list.Add((inDate, outDate));
			}

			var knownPassports = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			var guestBuffer = new CGuest[1024];
			int guestCount = Hotel_GetAllGuests(hotelPtr, guestBuffer, guestBuffer.Length);
			for (int i = 0; i < guestCount; i++)
			{
				var pass = (guestBuffer[i].Passport ?? string.Empty).Trim();
				if (pass.Length > 0) knownPassports.Add(pass);
			}

			var occupiedByPassport = new Dictionary<string, (DateTime In, DateTime Out)>(StringComparer.OrdinalIgnoreCase);
			foreach (var room in rooms)
			{
				if (room.Status != 1) continue;
				var pass = (room.GuestPassport ?? string.Empty).Trim();
				if (pass.Length == 0) continue;
				if (!TryNormalizeDates(room.CheckInDate, room.CheckOutDate, out var inDate, out var outDate, out _))
					continue;

				if (!occupiedByPassport.TryGetValue(pass, out var existing) || outDate > existing.Out)
					occupiedByPassport[pass] = (inDate, outDate);
			}

			var nameByPassport = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			var buildingNameByKey = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			var typeByRoom = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			var batchRangesByRoom = new Dictionary<string, List<(DateTime In, DateTime Out)>>(StringComparer.OrdinalIgnoreCase);

			foreach (var row in rows)
				Validate(row, rooms, buildings, bookingsByRoom,
					knownPassports, occupiedByPassport,
					nameByPassport, buildingNameByKey, typeByRoom, batchRangesByRoom);
		}

		// Применяет проверенные строки импорта к данным отеля.
		public static ImportSummary Apply(IEnumerable<ImportRow> rows, IntPtr hotelPtr)
		{
			var result = new ImportSummary();
			var buildings = ReadBuildings(hotelPtr);
			var rooms = ReadRooms(hotelPtr);

			foreach (var row in rows)
			{
				result.TotalRows++;
				if (row.HasError)
				{
					result.FailedRows++;
					continue;
				}

				int buildingId = FindBuildingId(buildings, row.Building);
				if (buildingId == 0)
				{
					Hotel_AddBuilding(hotelPtr, row.Building.Trim());
					buildings = ReadBuildings(hotelPtr);
					buildingId = FindBuildingId(buildings, row.Building);
					result.CreatedBuildings++;
				}

				var room = FindRoom(rooms, buildings, buildingId, row.RoomNumber);
				if (room.Id == 0)
				{
					Hotel_AddRoom(hotelPtr, buildingId, row.RoomTypeValue, row.RoomNumber.Trim(), row.Price);
					rooms = ReadRooms(hotelPtr);
					room = FindRoom(rooms, buildings, buildingId, row.RoomNumber);
					result.CreatedRooms++;
				}
				else
				{
					if (Math.Abs(room.BasePrice - row.Price) > 0.001)
						Hotel_UpdatePrice(hotelPtr, room.Id, row.Price);
				}

				if (room.Id == 0)
				{
					row.Error = "Не удалось создать номер в C++ модели.";
					result.FailedRows++;
					continue;
				}

				bool booked = Hotel_BookRoom(hotelPtr, room.Id, row.FullName.Trim(),
					row.Passport.Trim(), row.CheckIn.ToString("yyyy-MM-dd"), row.CheckOut.ToString("yyyy-MM-dd"));

				if (!booked)
				{
					row.Error = "C++ модель отклонила бронь (пересечение дат или некорректные данные).";
					result.FailedRows++;
					continue;
				}

				result.ImportedRows++;
			}

			return result;
		}

		// Проверяет одну строку импорта и заполняет её нормализованными данными или ошибкой.
		private static void Validate(ImportRow row,
			List<CRoom> rooms, Dictionary<int, string> buildings,
			Dictionary<string, List<(DateTime In, DateTime Out)>> bookingsByRoom,
			HashSet<string> knownPassports,
			Dictionary<string, (DateTime In, DateTime Out)> occupiedByPassport,
			Dictionary<string, string> nameByPassport,
			Dictionary<string, string> buildingNameByKey,
			Dictionary<string, int> typeByRoom,
			Dictionary<string, List<(DateTime In, DateTime Out)>> batchRangesByRoom)
		{
			row.Error = string.Empty;

			row.FullName = InputRules.NormalizeFullName(row.FullName);
			row.Passport = (row.Passport ?? string.Empty).Trim();
			row.Building = (row.Building ?? string.Empty).Trim();
			row.RoomType = (row.RoomType ?? string.Empty).Trim();
			row.RoomNumber = (row.RoomNumber ?? string.Empty).Trim();

			if (string.IsNullOrWhiteSpace(row.FullName))
			{
				row.Error = "Не указано ФИО гостя.";
				return;
			}
			if (!InputRules.IsValidFullName(row.FullName))
			{
				row.Error = "ФИО должно содержать только буквы, пробел и дефис (2–100 символов).";
				return;
			}

			if (!InputRules.TryNormalizePassport(row.Passport, out var passport))
			{
				row.Error = "Паспорт должен быть в формате «1234 567890».";
				return;
			}
			row.Passport = passport;

			if (string.IsNullOrWhiteSpace(row.Building))
			{
				row.Error = "Не указан корпус.";
				return;
			}
			if (!InputRules.IsValidBuildingName(row.Building))
			{
				row.Error = "Название корпуса слишком длинное (максимум 50 символов).";
				return;
			}

			int type = ParseType(row.RoomType);
			if (type < 0)
			{
				row.Error = "Тип номера должен быть: Стандарт, Люкс или Конференц-зал.";
				return;
			}
			row.RoomTypeValue = type;

			if (!InputRules.IsValidRoomNumber(row.RoomNumber))
			{
				row.Error = "Номер комнаты должен содержать только цифры (1–5 знаков).";
				return;
			}

			if (!TryParsePrice(row.PriceText, out var price))
			{
				row.Error = "Цена за сутки должна быть целым положительным числом.";
				return;
			}
			row.Price = price;

			if (!TryNormalizeDates(row.CheckInText, row.CheckOutText, out var inDate, out var outDate, out var dateError))
			{
				row.Error = dateError;
				return;
			}
			row.CheckIn = inDate;
			row.CheckOut = outDate;

			string passKey = row.Passport;
			if (!nameByPassport.TryGetValue(passKey, out var existingName))
			{
				nameByPassport[passKey] = row.FullName;
			}
			else if (!string.Equals(existingName, row.FullName, StringComparison.OrdinalIgnoreCase))
			{
				row.Error = "Для одного паспорта указаны разные ФИО в разных строках.";
				return;
			}

			string buildingKey = row.Building.Trim().ToLowerInvariant();
			if (!buildingNameByKey.TryGetValue(buildingKey, out var canonicalBuilding))
			{
				buildingNameByKey[buildingKey] = row.Building;
			}
			else if (!string.Equals(canonicalBuilding, row.Building, StringComparison.Ordinal))
			{
				row.Error = "Название одного и того же корпуса написано по-разному в разных строках.";
				return;
			}

			string key = RoomKey(row.Building, row.RoomNumber);
			if (!typeByRoom.TryGetValue(key, out var existingType))
			{
				typeByRoom[key] = type;
			}
			else if (existingType != type)
			{
				row.Error = "Для одного и того же номера указан разный тип в разных строках.";
				return;
			}

			var existingRoom = rooms.FirstOrDefault(r =>
				buildings.ContainsKey(r.BuildingId) &&
				string.Equals(buildings[r.BuildingId], row.Building, StringComparison.OrdinalIgnoreCase) &&
				string.Equals((r.Number ?? string.Empty).Trim(), row.RoomNumber, StringComparison.OrdinalIgnoreCase));

			if (existingRoom.Id != 0 && existingRoom.Type != type)
			{
				row.Error = "Номер уже существует в отеле с другим типом.";
				return;
			}

			if (occupiedByPassport.TryGetValue(passKey, out var occupied) && inDate < occupied.Out)
			{
				row.Error = "Этот гость уже проживает в указанный период (пересечение с текущим заселением).";
				return;
			}

			if (bookingsByRoom.TryGetValue(key, out var existingRanges))
			{
				foreach (var range in existingRanges)
				{
					if (DatesOverlap(inDate, outDate, range.In, range.Out))
					{
						row.Error = "Даты пересекаются с существующей бронью или проживанием в этом номере.";
						return;
					}
				}
			}

			if (!batchRangesByRoom.TryGetValue(key, out var batchRanges))
			{
				batchRanges = new List<(DateTime In, DateTime Out)>();
				batchRangesByRoom[key] = batchRanges;
			}

			foreach (var range in batchRanges)
			{
				if (DatesOverlap(inDate, outDate, range.In, range.Out))
				{
					row.Error = "Даты пересекаются с другой строкой импорта для этого номера.";
					return;
				}
			}

			batchRanges.Add((inDate, outDate));
		}

		// Считывает список корпусов из нативной модели отеля.
		private static Dictionary<int, string> ReadBuildings(IntPtr hotelPtr)
		{
			var result = new Dictionary<int, string>();
			var buffer = new CBuilding[256];
			int count = Hotel_GetAllBuildings(hotelPtr, buffer, buffer.Length);
			for (int i = 0; i < count; i++)
				result[buffer[i].Id] = buffer[i].Name ?? string.Empty;
			return result;
		}

		// Считывает список номеров из нативной модели отеля.
		private static List<CRoom> ReadRooms(IntPtr hotelPtr)
		{
			var buffer = new CRoom[1024];
			int count = Hotel_GetAllRooms(hotelPtr, buffer, buffer.Length);
			return buffer.Take(count).ToList();
		}

		// Считывает список бронирований из нативной модели отеля.
		private static List<CBooking> ReadBookings(IntPtr hotelPtr)
		{
			var buffer = new CBooking[2048];
			int count = Hotel_GetAllBookings(hotelPtr, buffer, buffer.Length);
			return buffer.Take(count).ToList();
		}

		// Пытается разобрать и нормализовать даты заезда и выезда.
		private static bool TryNormalizeDates(string inText, string outText, out DateTime inDate, out DateTime outDate, out string error)
		{
			inDate = default(DateTime);
			outDate = default(DateTime);
			error = string.Empty;

			if (!InputRules.TryParseDateText(inText, out inDate))
			{
				error = "Дата заезда должна быть в формате дд.мм.гггг.";
				return false;
			}

			if (!InputRules.TryParseDateText(outText, out outDate))
			{
				error = "Дата выезда должна быть в формате дд.мм.гггг.";
				return false;
			}

			if (outDate <= inDate)
			{
				error = "Дата выезда должна быть позже даты заезда.";
				return false;
			}

			return true;
		}

		// Пытается разобрать цену за сутки из текстового значения.
		private static bool TryParsePrice(string text, out double price)
		{
			price = 0;
			if (!InputRules.IsValidPrice(text, out var intPrice))
				return false;
			price = intPrice;
			return true;
		}

		// Формирует нормализованный ключ номера по корпусу и номеру комнаты.
		private static string RoomKey(string building, string number)
		{
			return (building ?? string.Empty).Trim().ToLowerInvariant() + "|" +
				(number ?? string.Empty).Trim().ToLowerInvariant();
		}

		// Преобразует текстовое название типа номера в числовой код.
		private static int ParseType(string value)
		{
			string v = (value ?? string.Empty).Trim().ToLowerInvariant().Replace('ё', 'е');
			if (v == "стандарт" || v == "standard") return 0;
			if (v == "люкс" || v == "lux") return 1;
			if (v == "конференц-зал" || v == "конференц зал" || v == "conference") return 2;
			return -1;
		}

		// Возвращает вместимость номера по типу для справочных операций.
		private static int GetTypeCapacity(int type)
		{
			switch (type)
			{
				case 1: return 2;
				case 2: return 20;
				default: return 2;
			}
		}

		// Возвращает текстовое название типа номера по числовому коду.
		private static string FormatType(int type)
		{
			switch (type)
			{
				case 1: return "Люкс";
				case 2: return "Конференц-зал";
				default: return "Стандарт";
			}
		}

		// Ищет идентификатор корпуса по названию без учёта регистра.
		private static int FindBuildingId(Dictionary<int, string> buildings, string name)
		{
			foreach (var pair in buildings)
			{
				if (string.Equals(pair.Value, name, StringComparison.OrdinalIgnoreCase))
					return pair.Key;
			}
			return 0;
		}

		// Ищет номер в списке по корпусу и отображаемому номеру комнаты.
		private static CRoom FindRoom(List<CRoom> rooms, Dictionary<int, string> buildings, int buildingId, string number)
		{
			foreach (var room in rooms)
			{
				if (room.BuildingId != buildingId) continue;
				if (string.Equals((room.Number ?? string.Empty).Trim(), number.Trim(), StringComparison.OrdinalIgnoreCase))
					return room;
			}
			return default(CRoom);
		}

		// Проверяет пересечение двух диапазонов дат проживания.
		private static bool DatesOverlap(DateTime aIn, DateTime aOut, DateTime bIn, DateTime bOut)
		{
			return aIn < bOut && bIn < aOut;
		}

		// Вычисляет количество дней между датами заезда и выезда.
		private static int CountDays(DateTime inDate, DateTime outDate)
		{
			return Math.Max(1, (int)Math.Ceiling((outDate.Date - inDate.Date).TotalDays));
		}
	}
}
