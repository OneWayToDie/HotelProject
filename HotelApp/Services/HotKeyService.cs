using System.Windows.Input;
using HotelApp.ViewModels;

namespace HotelApp.Services
{
	public static class HotkeyService
	{
		// Регистрирует горячие клавиши главного окна на основе сохранённых настроек.
		public static void Register(MainWindow window, MainViewModel vm)
		{
			var settings = AppSettings.Load();
			window.InputBindings.Clear();

			var converter = new KeyGestureConverter();

			// Добавляет привязку команды к сочетанию клавиш для указанного действия.
			void AddBinding(string actionKey, ICommand command)
			{
				if (settings.Hotkeys.TryGetValue(actionKey, out string gestureStr))
				{
					try
					{
						var gesture = (KeyGesture)converter.ConvertFromString(gestureStr);
						window.InputBindings.Add(new InputBinding(command, gesture));
					}
					catch
					{
						System.Diagnostics.Debug.WriteLine($"Неверный формат клавиши: {gestureStr}");
					}
				}
			}

			AddBinding("SaveData", new RelayCommand(_ => vm.SaveData()));
			AddBinding("Refresh", new RelayCommand(_ => window.TriggerRefresh()));
			AddBinding("ImportExcel", new RelayCommand(_ => window.TriggerImportExcel()));
			AddBinding("AddBuilding", new RelayCommand(_ => window.TriggerAddBuilding()));
			AddBinding("AddRoom", new RelayCommand(_ => window.TriggerAddRoom()));
			AddBinding("About", new RelayCommand(_ => window.TriggerAbout()));
		}
	}
}
