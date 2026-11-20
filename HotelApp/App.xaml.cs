using System;
using System.Windows;
using HotelApp.Services;

namespace HotelApp
{
	public partial class App : Application
	{
		// Применяет сохранённые настройки оформления перед созданием главного окна.
		protected override void OnStartup(StartupEventArgs e)
		{
			try
			{
				var settings = AppSettings.Load();
				ThemeService.Apply(settings.Theme);
				AnimationService.Initialize(settings.EnableAnimations);
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine("Не удалось применить настройки оформления: " + ex.Message);
			}

			base.OnStartup(e);
		}
	}
}