using System;
using System.Collections.Generic;
using HotelApp.ViewModels;

namespace HotelApp.Services
{
	// Генератор шаблона и экспорт таблицы гостей в формате импорта Excel
	public static class ExcelTemplate
	{
		public const string DefaultFileName = "Шаблон_гостей_20_чел.xlsx";

		public static void Save(string path)
		{
			XlsxFile.Write(path, "Гости", ExcelImportService.Headers, BuildDemoRows());
		}

		// Сохраняет текущие данные гостей (все проживания/брони) в таблицу Excel,
		// совместимую с импортом. Если записей нет — сохраняется пустая таблица с шапкой.
		public static void SaveCurrentData(string path, IntPtr hotelPtr)
		{
			XlsxFile.Write(path, "Гости", ExcelImportService.Headers, BuildCurrentDataRows(hotelPtr));
		}

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

		public static List<object[]> BuildDemoRows()
		{
			// Корпуса: «Главный корпус» и «Корпус А» созданы по умолчанию, «Корпус Б» создастся при импорте.
			// Внутри одного номера брони не пересекаются — файл проходит проверку сразу.
			var rows = new List<object[]>
			{
				Row(1, "Иванов Иван Иванович", "4512 345678", "Главный корпус", "Стандарт", "101", 2500, "01.12.2026", "05.12.2026"),
				Row(2, "Петрова Анна Сергеевна", "4513 456789", "Главный корпус", "Стандарт", "101", 2500, "05.12.2026", "09.12.2026"),
				Row(3, "Смирнов Алексей Петрович", "4514 567890", "Главный корпус", "Стандарт", "101", 2500, "10.12.2026", "14.12.2026"),

				Row(4, "Кузнецова Мария Ивановна", "4515 678901", "Главный корпус", "Стандарт", "102", 2800, "02.12.2026", "06.12.2026"),
				Row(5, "Попов Дмитрий Николаевич", "4516 789012", "Главный корпус", "Стандарт", "102", 2800, "07.12.2026", "11.12.2026"),
				Row(6, "Соколов Андрей Викторович", "4517 890123", "Главный корпус", "Стандарт", "102", 2800, "12.12.2026", "16.12.2026"),

				Row(7, "Лебедева Ольга Дмитриевна", "4518 901234", "Главный корпус", "Люкс", "103", 6000, "01.12.2026", "04.12.2026"),
				Row(8, "Козлов Сергей Александрович", "4519 012345", "Главный корпус", "Люкс", "103", 6000, "05.12.2026", "08.12.2026"),
				Row(9, "Новикова Елена Викторовна", "4520 123456", "Главный корпус", "Люкс", "103", 6000, "10.12.2026", "13.12.2026"),

				Row(10, "Морозов Павел Андреевич", "4521 234567", "Главный корпус", "Конференц-зал", "104", 10000, "01.12.2026", "03.12.2026"),
				Row(11, "Волкова Татьяна Юрьевна", "4522 345678", "Главный корпус", "Конференц-зал", "104", 10000, "04.12.2026", "06.12.2026"),
				Row(12, "Зайцев Никита Олегович", "4523 456789", "Главный корпус", "Конференц-зал", "104", 10000, "08.12.2026", "10.12.2026"),
				Row(13, "Фёдорова Ирина Максимовна", "4524 567890", "Главный корпус", "Конференц-зал", "104", 10000, "11.12.2026", "13.12.2026"),

				Row(14, "Михайлов Артём Валерьевич", "4525 678901", "Корпус Б", "Стандарт", "201", 2200, "01.12.2026", "05.12.2026"),
				Row(15, "Белова Наталья Игоревна", "4526 789012", "Корпус Б", "Стандарт", "201", 2200, "06.12.2026", "10.12.2026"),
				Row(16, "Тихонов Роман Сергеевич", "4527 890123", "Корпус Б", "Стандарт", "201", 2200, "11.12.2026", "15.12.2026"),

				Row(17, "Гусева Светлана Анатольевна", "4528 901234", "Корпус Б", "Люкс", "202", 5500, "02.12.2026", "06.12.2026"),
				Row(18, "Егоров Максим Дмитриевич", "4529 012345", "Корпус Б", "Люкс", "202", 5500, "07.12.2026", "11.12.2026"),
				Row(19, "Павлова Екатерина Андреевна", "4530 123456", "Корпус Б", "Люкс", "202", 5500, "12.12.2026", "16.12.2026"),

				Row(20, "Романов Владислав Игоревич", "4531 234567", "Главный корпус", "Стандарт", "101", 2500, "15.12.2026", "19.12.2026")
			};

			return rows;
		}

		private static object[] Row(int number, string fullName, string passport, string building,
			string roomType, string roomNumber, double price, string checkIn, string checkOut)
		{
			return new object[]
			{
				number,
				fullName,
				passport,
				building,
				roomType,
				roomNumber,
				price,
				DateTime.ParseExact(checkIn, "dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture),
				DateTime.ParseExact(checkOut, "dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture)
			};
		}
	}
}
