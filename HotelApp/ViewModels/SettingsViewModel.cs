using System.Collections.Generic;
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
		private string _gestureBeforeRecording;
		private HotkeySetting _recordingSetting;
		private string _selectedTheme = ThemeService.DefaultTheme;
		private bool _isAnimationsEnabled = true;

		// Получает коллекцию настраиваемых горячих клавиш.
		public ObservableCollection<HotkeySetting> Hotkeys { get; } = new ObservableCollection<HotkeySetting>();

		// Получает или задаёт текущую строку, находящуюся в режиме записи сочетания.
		public HotkeySetting RecordingSetting
		{
			get => _recordingSetting;
			set
			{
				_recordingSetting = value;
				OnPropertyChanged();
			}
		}

		// Получает текст текущего статуса настроек.
		public string StatusMessage { get; private set; } = "Готово";

		// Получает выбранную тему оформления приложения.
		public string SelectedTheme
		{
			get => _selectedTheme;
			private set
			{
				if (_selectedTheme == value) return;
				_selectedTheme = value;
				OnPropertyChanged();
			}
		}

		// Получает или задаёт признак включения анимаций интерфейса.
		public bool IsAnimationsEnabled
		{
			get => _isAnimationsEnabled;
			set
			{
				if (_isAnimationsEnabled == value) return;
				_isAnimationsEnabled = value;
				AnimationService.SetEnabled(value);
				OnPropertyChanged();
			}
		}

		// Создаёт модель представления настроек приложения.
		public SettingsViewModel(MainWindow window, MainViewModel mainVm)
		{
			_window = window;
			_mainVm = mainVm;
			LoadFromSettings();
		}

		// Загружает текущие настройки из файла конфигурации.
		private void LoadFromSettings()
		{
			var settings = AppSettings.Load();
			Hotkeys.Clear();

			_selectedTheme = ThemeService.Normalize(settings.Theme);
			_isAnimationsEnabled = settings.EnableAnimations;
			OnPropertyChanged(nameof(SelectedTheme));
			OnPropertyChanged(nameof(IsAnimationsEnabled));

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

		// Переводит выбранное действие в режим записи сочетания клавиш.
		public void StartRecording(HotkeySetting setting)
		{
			if (setting == null || setting == RecordingSetting) return;

			if (RecordingSetting != null)
			{
				RecordingSetting.IsRecording = false;
				RecordingSetting.Gesture = _gestureBeforeRecording;
			}

			_gestureBeforeRecording = setting.Gesture;
			setting.Gesture = string.Empty;
			setting.IsRecording = true;
			RecordingSetting = setting;
			StatusMessage = $"Запись для «{setting.DisplayName}»: нажмите комбинацию...";
			OnPropertyChanged(nameof(StatusMessage));
		}

		// Применяет выбранную тему оформления в режиме предварительного просмотра.
		public void SelectTheme(string themeName)
		{
			string normalized = ThemeService.Normalize(themeName);
			SelectedTheme = normalized;
			ThemeService.Apply(normalized);
			StatusMessage = "Тема применена — нажмите «Сохранить», чтобы запомнить.";
			OnPropertyChanged(nameof(StatusMessage));
		}

		// Обрабатывает нажатие клавиш в режиме записи сочетания и возвращает признак обработки.
		public bool HandleKeyDown(KeyEventArgs e)
		{
			if (RecordingSetting == null) return false;

			var key = e.Key == Key.System ? e.SystemKey : e.Key;
			if (key == Key.None) return false;

			if (key == Key.LeftCtrl || key == Key.RightCtrl ||
				key == Key.LeftShift || key == Key.RightShift ||
				key == Key.LeftAlt || key == Key.RightAlt ||
				key == Key.LWin || key == Key.RWin ||
				key == Key.System)
				return false;

			var recording = RecordingSetting;

			if (key == Key.Escape)
			{
				recording.Gesture = _gestureBeforeRecording;
				recording.IsRecording = false;
				RecordingSetting = null;
				StatusMessage = "Запись отменена — комбинация не изменена.";
				OnPropertyChanged(nameof(StatusMessage));
				return true;
			}

			string gesture = BuildGesture(key, Keyboard.Modifiers);

			var duplicate = Hotkeys.FirstOrDefault(h =>
				h.ActionKey != recording.ActionKey &&
				string.Equals(h.Gesture, gesture, System.StringComparison.OrdinalIgnoreCase));

			if (duplicate != null)
			{
				recording.Gesture = _gestureBeforeRecording;
				StatusMessage = $"⚠ Комбинация занята: «{duplicate.DisplayName}». Прежняя комбинация сохранена.";
				OnPropertyChanged(nameof(StatusMessage));
				recording.IsRecording = false;
				RecordingSetting = null;
				return true;
			}

			recording.Gesture = gesture;
			recording.IsRecording = false;
			StatusMessage = $"✓ Записано: {gesture}";
			OnPropertyChanged(nameof(StatusMessage));
			RecordingSetting = null;
			return true;
		}

		// Сохраняет текущие настройки и применяет их в приложении.
		public void Save()
		{
			var settings = AppSettings.Load();
			foreach (var h in Hotkeys)
			{
				if (string.IsNullOrWhiteSpace(h.Gesture) || h.Gesture == "—") continue;
				settings.Hotkeys[h.ActionKey] = h.Gesture;
			}

			settings.Theme = SelectedTheme;
			settings.EnableAnimations = IsAnimationsEnabled;

			AppSettings.Save(settings);
			HotkeyService.Register(_window, _mainVm);
			ThemeService.Apply(SelectedTheme);
			AnimationService.SetEnabled(IsAnimationsEnabled);

			_mainVm.AddLog("Настройки сохранены: горячие клавиши, тема оформления, анимации.", LogType.Success);
			StatusMessage = "✓ Настройки сохранены и применены";
			OnPropertyChanged(nameof(StatusMessage));
		}

		// Сбрасывает настройки к значениям по умолчанию без сохранения в файл.
		public void ResetToDefaults()
		{
			var defaults = new AppSettings();
			foreach (var h in Hotkeys)
			{
				if (defaults.Hotkeys.TryGetValue(h.ActionKey, out var g))
					h.Gesture = g;
			}

			SelectedTheme = ThemeService.DefaultTheme;
			ThemeService.Apply(SelectedTheme);
			IsAnimationsEnabled = true;

			StatusMessage = "Сброшено к значениям по умолчанию (не забудьте сохранить)";
			OnPropertyChanged(nameof(StatusMessage));
		}

		// Формирует строковое представление сочетания клавиш.
		private static string BuildGesture(Key key, ModifierKeys modifiers)
		{
			var parts = new List<string>();
			if ((modifiers & ModifierKeys.Control) != 0) parts.Add("Ctrl");
			if ((modifiers & ModifierKeys.Shift) != 0) parts.Add("Shift");
			if ((modifiers & ModifierKeys.Alt) != 0) parts.Add("Alt");
			if ((modifiers & ModifierKeys.Windows) != 0) parts.Add("Windows");

			parts.Add(key.ToString());
			return string.Join("+", parts);
		}

		// Событие уведомления об изменении значения свойства.
		public event PropertyChangedEventHandler PropertyChanged;

		// Уведомляет подписчиков об изменении свойства.
		protected void OnPropertyChanged([CallerMemberName] string name = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}
	}
}
