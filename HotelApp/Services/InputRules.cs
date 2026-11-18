using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace HotelApp.Services
{
	// Единые правила проверки данных, которые вводит пользователь.
	// Используются и диалогами, и импортом Excel.
	public static class InputRules
	{
		public const int MaxFullNameLength = 100;
		public const int MaxBuildingNameLength = 50;
		public const int MaxRoomNumberLength = 5;
		public const int MaxPriceDigits = 9;

		private static readonly Regex FullNameRegex = new Regex(
			@"^[А-Яа-яЁёA-Za-z]+(?:[ -][А-Яа-яЁёA-Za-z]+)*$",
			RegexOptions.Compiled | RegexOptions.CultureInvariant);

		private static readonly Regex PassportRegex = new Regex(
			@"^\d{4} \d{6}$",
			RegexOptions.Compiled | RegexOptions.CultureInvariant);

		private static readonly Regex RoomNumberRegex = new Regex(
			@"^\d{1,5}$",
			RegexOptions.Compiled | RegexOptions.CultureInvariant);

		private static readonly Regex DateRegex = new Regex(
			@"^\d{2}\.\d{2}\.\d{4}$",
			RegexOptions.Compiled | RegexOptions.CultureInvariant);

		private static readonly Regex PriceRegex = new Regex(
			@"^\d{1,9}$",
			RegexOptions.Compiled | RegexOptions.CultureInvariant);

		// ФИО: только буквы, пробел и дефис. Цифры и знаки не допускаются.
		public static string NormalizeFullName(string value)
		{
			if (string.IsNullOrWhiteSpace(value)) return string.Empty;
			return Regex.Replace(value.Trim(), @"\s+", " ");
		}

		public static bool IsValidFullName(string value)
		{
			string text = NormalizeFullName(value);
			return text.Length >= 2 && text.Length <= MaxFullNameLength && FullNameRegex.IsMatch(text);
		}

		// Паспорт: строго 4 цифры, пробел, 6 цифр.
		public static bool IsValidPassport(string value)
		{
			return PassportRegex.IsMatch((value ?? string.Empty).Trim());
		}

		// Строгий разбор паспорта для импорта: "4512 345678" или 10 цифр подряд.
		// Старый формат "45 12 345678" и любые лишние знаки отклоняются.
		public static bool TryNormalizePassport(string value, out string normalized)
		{
			normalized = (value ?? string.Empty).Trim();
			if (PassportRegex.IsMatch(normalized)) return true;

			// Если в строке есть пробелы, формат должен быть строго «NNNN NNNNNN»
			if (normalized.IndexOf(' ') >= 0 || normalized.IndexOf('\u00A0') >= 0)
			{
				normalized = string.Empty;
				return false;
			}

			string digits = string.Empty;
			foreach (char c in normalized)
			{
				if (char.IsDigit(c)) { digits += c; continue; }

				normalized = string.Empty;
				return false;
			}

			if (digits.Length != 10)
			{
				normalized = string.Empty;
				return false;
			}

			normalized = digits.Substring(0, 4) + " " + digits.Substring(4);
			return true;
		}

		// Приводит 10 цифр к виду "4512 345678". Если цифр не 10 — возвращает то, что есть.
		public static string FormatPassportDigits(string value)
		{
			string digits = string.Empty;
			foreach (char c in value ?? string.Empty)
				if (char.IsDigit(c)) digits += c;

			if (digits.Length > 10) digits = digits.Substring(0, 10);
			if (digits.Length <= 4) return digits;
			return digits.Substring(0, 4) + " " + digits.Substring(4);
		}

		// Номер комнаты: только цифры, 1–5 знаков.
		public static bool IsValidRoomNumber(string value)
		{
			return RoomNumberRegex.IsMatch((value ?? string.Empty).Trim());
		}

		// Название корпуса: любая непустая строка до 50 символов.
		public static bool IsValidBuildingName(string value)
		{
			string text = (value ?? string.Empty).Trim();
			return text.Length > 0 && text.Length <= MaxBuildingNameLength;
		}

		// Цена: только целые рубли, больше нуля.
		public static bool IsValidPrice(string value, out int price)
		{
			price = 0;
			string text = (value ?? string.Empty).Trim();
			if (!PriceRegex.IsMatch(text)) return false;
			return int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out price) && price > 0;
		}

		// Дата в формате дд.мм.гггг (буквы и другие разделители не допускаются).
		public static bool TryParseDateText(string value, out DateTime date)
		{
			date = DateTime.MinValue;
			string text = (value ?? string.Empty).Trim();
			if (!DateRegex.IsMatch(text)) return false;
			return DateTime.TryParseExact(text, "dd.MM.yyyy", CultureInfo.InvariantCulture,
				DateTimeStyles.None, out date);
		}

		// Текст даты из DatePicker: цифры и точки, не больше 10 символов.
		public static bool IsAllowedDateInput(string text, string added)
		{
			foreach (char c in added ?? string.Empty)
				if (!char.IsDigit(c) && c != '.') return false;

			// Не даём выйти за формат дд.мм.гггг
			int digitCount = 0;
			foreach (char c in (text ?? string.Empty) + (added ?? string.Empty))
				if (char.IsDigit(c)) digitCount++;
			return digitCount <= 8;
		}

		// Разрешённые символы для ФИО.
		public static bool IsAllowedNameInput(string added)
		{
			foreach (char c in added ?? string.Empty)
			{
				if (char.IsLetter(c)) continue;
				if (c == ' ' || c == '-') continue;
				return false;
			}
			return true;
		}
	}
}
