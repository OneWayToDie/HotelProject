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

		public ObservableCollection<HotkeySetting> Hotkeys { get; } = new ObservableCollection<HotkeySetting>();

		// Комбинация, которая была в поле до начала записи — для отмены по Esc
		private string _gestureBeforeRecording;

		// Какая строка сейчас в режиме записи (только одна одновременно)
		private HotkeySetting _recordingSetting;
		public HotkeySetting RecordingSetting
		{
			get => _recordingSetting;
			set { _recordingSetting = value; OnPropertyChanged(); }
		}

		public string StatusMessage { get; private set; } = "Готово";

		// Выбранная тема оформления (Indigo, Emerald, Ruby, Amber)
		private string _selectedTheme = ThemeService.DefaultTheme;
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

		// Плавные анимации: применяются сразу, сохраняются в settings.json
		private bool _isAnimationsEnabled = true;
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

		public void StartRecording(HotkeySetting setting)
		{
			if (setting == null || setting == RecordingSetting) return;

			// Если запись уже шла в другой строке — возвращаем ей прежнюю комбинацию
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

		// Применяем тему сразу (живой предпросмотр), в settings.json попадает по кнопке «Сохранить».
		public void SelectTheme(string themeName)
		{
			string normalized = ThemeService.Normalize(themeName);
			SelectedTheme = normalized;
			ThemeService.Apply(normalized);
			StatusMessage = "Тема применена — нажмите «Сохранить», чтобы запомнить.";
			OnPropertyChanged(nameof(StatusMessage));
		}

		// Возвращает true, если нажатие обработано и его нельзя передавать дальше
		// (иначе, например, Space нажал бы кнопку «Записать» ещё раз).
		public bool HandleKeyDown(KeyEventArgs e)
		{
			if (RecordingSetting == null) return false;

			// Alt+... приходит как Key.System, настоящая клавиша лежит в SystemKey
			var key = e.Key == Key.System ? e.SystemKey : e.Key;
			if (key == Key.None) return false;

			// Одиночные модификаторы ничего не записываем
			if (key == Key.LeftCtrl || key == Key.RightCtrl ||
				key == Key.LeftShift || key == Key.RightShift ||
				key == Key.LeftAlt || key == Key.RightAlt ||
				key == Key.LWin || key == Key.RWin ||
				key == Key.System)
				return false;

			var recording = RecordingSetting;

			// Esc отменяет запись и оставляет прежнюю комбинацию
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

		// Собирает комбинацию в формате, который понимает KeyGestureConverter: Ctrl+Shift+K
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

		public void Save()
		{
			var settings = AppSettings.Load();
			foreach (var h in Hotkeys)
			{
				// Пустые значения и заглушка «—» не должны попадать в settings.json
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

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string name = null)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}
}