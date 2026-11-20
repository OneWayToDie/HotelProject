using System;
using System.Linq;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;

namespace HotelApp.Services
{
	// Применение цветовых тем MaterialDesign через PaletteHelper (API v4.9.0).
	public static class ThemeService
	{
		public const string DefaultTheme = "Indigo";

		public static readonly string[] AvailableThemes = { "Indigo", "Emerald", "Ruby", "Amber" };

		public static void Apply(string themeName)
		{
			try
			{
				string name = Normalize(themeName);
				var colors = GetColors(name);

				var helper = new PaletteHelper();
				var theme = helper.GetTheme();
				theme.SetPrimaryColor(colors.Primary);
				theme.SetSecondaryColor(colors.Secondary);
				helper.SetTheme(theme);
			}
			catch (Exception ex)
			{
				// Тема не должна ронять приложение — просто остаёмся на текущей.
				System.Diagnostics.Debug.WriteLine("Не удалось применить тему: " + ex.Message);
			}
		}

		public static string Normalize(string themeName)
		{
			if (string.IsNullOrWhiteSpace(themeName)) return DefaultTheme;
			var match = AvailableThemes.FirstOrDefault(t =>
				string.Equals(t, themeName.Trim(), StringComparison.OrdinalIgnoreCase));
			return match ?? DefaultTheme;
		}

		// Цвет для кружка-образца в настройках.
		public static Color GetSwatch(string themeName) => GetColors(Normalize(themeName)).Primary;

		private static (Color Primary, Color Secondary) GetColors(string name)
		{
			switch (name)
			{
				case "Emerald":
					return (Color.FromRgb(0x2E, 0x7D, 0x32), Color.FromRgb(0xFF, 0xB3, 0x00));
				case "Ruby":
					return (Color.FromRgb(0xC6, 0x28, 0x28), Color.FromRgb(0xFF, 0x8F, 0x00));
				case "Amber":
					return (Color.FromRgb(0xFF, 0x8F, 0x00), Color.FromRgb(0x45, 0x5A, 0x64));
				default: // Indigo
					return (Color.FromRgb(0x3F, 0x51, 0xB5), Color.FromRgb(0xFF, 0xC1, 0x07));
			}
		}
	}
}
