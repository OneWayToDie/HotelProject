using System;
using System.Windows;
using System.Windows.Threading;
using HotelApp.Services;

namespace HotelApp
{
	public partial class App : Application
	{
		// Применяем сохранённую тему и настройку анимаций до создания главного окна.
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