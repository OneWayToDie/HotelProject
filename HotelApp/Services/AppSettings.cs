using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace HotelApp.Services
{
	public class AppSettings
	{
		// Получает или задаёт сопоставление действий и сочетаний горячих клавиш.
		public Dictionary<string, string> Hotkeys { get; set; } = new Dictionary<string, string>
		{
			{ "Refresh", "F5" },
			{ "ImportExcel", "Ctrl+E" },
			{ "AddBuilding", "Ctrl+B" },
			{ "AddRoom", "Ctrl+N" },
			{ "SaveData", "Ctrl+S" },
			{ "About", "F1" }
		};

		// Получает или задаёт имя выбранной темы оформления.
		public string Theme { get; set; } = ThemeService.DefaultTheme;

		// Получает или задаёт признак включения анимаций интерфейса.
		public bool EnableAnimations { get; set; } = true;

		// Возвращает путь к файлу настроек приложения.
		private static string FilePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

		// Загружает настройки приложения из файла или создаёт настройки по умолчанию.
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
					return new AppSettings();
				}
			}

			var defaults = new AppSettings();
			Save(defaults);
			return defaults;
		}

		// Сохраняет настройки приложения в файл.
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
