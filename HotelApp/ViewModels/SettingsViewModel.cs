using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using HotelApp.Models;
using HotelApp.Services;

namespace HotelApp.ViewModels
{
	public class SettingsViewModel : INotifyPropertyChanged
	{
		private readonly MainWindow _window;
		private readonly MainViewModel _mainVm;

		public ObservableCollection<HotkeySetting> Hotkeys { get; } = new ObservableCollection<HotkeySetting>();

		// Какая строка сейчас в режиме записи (только одна одновременно)
		private HotkeySetting _recordingSetting;
		public HotkeySetting RecordingSetting
		{
			get => _recordingSetting;
			set { _recordingSetting = value; OnPropertyChanged(); }
		}

		public string StatusMessage { get; private set; } = "Готово";

		public SettingsViewModel(MainWindow window, MainViewModel mainVm)
		{
			_window = window;
			_mainVm = mainVm;
			LoadFromSettings();
		}

		private void LoadFromSettings()
		{
			var settings = AppSettings.Load();
			Hotkeys.Clear();

			var displayNames = new[]
			{
				("Refresh", "Обновить экран"),
				("ImportExcel", "Импорт Excel"),
				("AddBuilding", "Добавить корпус"),
				("AddRoom", "Добавить номер"),
				("SaveData", "Сохранить данные"),
				("About", "О программе")
			};

			foreach (var (key, name) in displayNames)
			{
				string gesture = settings.Hotkeys.TryGetValue(key, out var g) ? g : "—";
				Hotkeys.Add(new HotkeySetting
				{
					ActionKey = key,
					DisplayName = name,
					Gesture = gesture
				});
			}
		}

		public void StartRecording(HotkeySetting setting)
		{
			if (RecordingSetting != null && RecordingSetting != setting)
				RecordingSetting.IsRecording = false;

			setting.IsRecording = true;
			RecordingSetting = setting;
			StatusMessage = $"Запись для «{setting.DisplayName}»: нажмите комбинацию...";
			OnPropertyChanged(nameof(StatusMessage));
		}

		public void HandleKeyDown(KeyEventArgs e)
		{
			if (RecordingSetting == null) return;

			var key = e.Key;
			if (key == Key.LeftCtrl || key == Key.RightCtrl ||
				key == Key.LeftShift || key == Key.RightShift ||
				key == Key.LeftAlt || key == Key.RightAlt ||
				key == Key.LWin || key == Key.RWin)
				return;

			var modifiers = Keyboard.Modifiers;
			string gesture;

			if (modifiers == ModifierKeys.None)
				gesture = key.ToString();
			else
				gesture = $"{modifiers}+{key}";

			gesture = gesture.Replace("Control", "Ctrl")
							 .Replace("Shift", "Shift")
							 .Replace("Alt", "Alt");

			var duplicate = Hotkeys.FirstOrDefault(h =>
				h.ActionKey != RecordingSetting.ActionKey &&
				h.Gesture.Equals(gesture, System.StringComparison.OrdinalIgnoreCase));

			if (duplicate != null)
			{
				StatusMessage = $"⚠ Комбинация уже занята: «{duplicate.DisplayName}»";
				OnPropertyChanged(nameof(StatusMessage));
				RecordingSetting.IsRecording = false;
				RecordingSetting = null;
				return;
			}

			RecordingSetting.Gesture = gesture;
			RecordingSetting.IsRecording = false;
			StatusMessage = $"✓ Записано: {gesture}";
			OnPropertyChanged(nameof(StatusMessage));
			RecordingSetting = null;
		}

		public void Save()
		{
			var settings = AppSettings.Load();
			foreach (var h in Hotkeys)
				settings.Hotkeys[h.ActionKey] = h.Gesture;

			AppSettings.Save(settings);
			HotkeyService.Register(_window, _mainVm);

			_mainVm.AddLog("Настройки горячих клавиш сохранены.", LogType.Success);
			StatusMessage = "✓ Настройки сохранены и применены";
			OnPropertyChanged(nameof(StatusMessage));
		}

		public void ResetToDefaults()
		{
			var defaults = new AppSettings();
			foreach (var h in Hotkeys)
			{
				if (defaults.Hotkeys.TryGetValue(h.ActionKey, out var g))
					h.Gesture = g;
			}
			StatusMessage = "Сброшено к значениям по умолчанию (не забудьте сохранить)";
			OnPropertyChanged(nameof(StatusMessage));
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string name = null)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}
}