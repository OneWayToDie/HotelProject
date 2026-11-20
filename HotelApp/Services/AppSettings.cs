using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace HotelApp.Services
{
	public class AppSettings
	{
		// Словарь горячих клавиш. Ключ — действие, Значение — комбинация (формат WPF)
		public Dictionary<string, string> Hotkeys { get; set; } = new Dictionary<string, string>
		{
			{ "Refresh", "F5" },
			{ "ImportExcel", "Ctrl+E" },
			{ "AddBuilding", "Ctrl+B" },
			{ "AddRoom", "Ctrl+N" },
			{ "SaveData", "Ctrl+S" },
			{ "About", "F1" }
		};

		// Тема оформления: Indigo, Emerald, Ruby или Amber
		public string Theme { get; set; } = ThemeService.DefaultTheme;

		// Плавные анимации интерфейса (появление вкладок и карточек)
		public bool EnableAnimations { get; set; } = true;

		// Путь к файлу настроек (Вариант А: в папке с exe)
		private static string FilePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

		// Загрузка настроек (если файла нет — создаёт дефолтный)
		public static AppSettings Load()
		{
			if (File.Exists(FilePath))
			{
				try
				{
					string json = File.ReadAllText(FilePath);
					return JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
				}
				catch
				{
					return new AppSettings(); // Если файл битый, возвращаем дефолт
				}
			}

			// Файла нет — создаём дефолтный
			var defaults = new AppSettings();
			Save(defaults);
			return defaults;
		}

		// Сохранение настроек в JSON
		public static void Save(AppSettings settings)
		{
			try
			{
				string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
				File.WriteAllText(FilePath, json);
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine("Ошибка сохранения настроек: " + ex.Message);
			}
		}
	}
}