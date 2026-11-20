using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using HotelApp.Models;
using HotelApp.ViewModels;

namespace HotelApp.Views
{
	public partial class SettingsView : UserControl
	{
		private bool _syncingTheme;

		// Инициализирует представление настроек приложения.
		public SettingsView()
		{
			InitializeComponent();
			Loaded += SettingsView_Loaded;
		}

		// Синхронизирует элементы управления после загрузки представления.
		private void SettingsView_Loaded(object sender, RoutedEventArgs e)
		{
			try
			{
				SyncThemeButtons();
			}
			catch (Exception ex)
			{
				Services.AppDialog.Error("Ошибка загрузки настроек: " + ex.Message, "Ошибка");
			}
		}

		// Синхронизирует выбранную радиокнопку с текущей темой оформления.
		private void SyncThemeButtons()
		{
			var vm = DataContext as SettingsViewModel;
			if (vm == null || ThemePanel == null) return;

			_syncingTheme = true;
			try
			{
				foreach (var child in ThemePanel.Children)
				{
					var radio = child as RadioButton;
					if (radio == null) continue;
					radio.IsChecked = string.Equals(radio.Tag as string, vm.SelectedTheme,
						StringComparison.OrdinalIgnoreCase);
				}
			}
			finally
			{
				_syncingTheme = false;
			}
		}

		// Применяет выбранную пользователем тему оформления.
		private void Theme_Checked(object sender, RoutedEventArgs e)
		{
			try
			{
				if (_syncingTheme) return;

				var vm = DataContext as SettingsViewModel;
				var radio = sender as RadioButton;
				if (vm == null || radio == null || radio.IsChecked != true) return;

				if (radio.Tag is string themeName)
					vm.SelectTheme(themeName);
			}
			catch (Exception ex)
			{
				Services.AppDialog.Error("Ошибка применения темы: " + ex.Message, "Ошибка");
			}
		}

		// Переводит выбранную горячую клавишу в режим записи.
		private void RecordButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (sender is Button btn && btn.Tag is HotkeySetting setting && DataContext is SettingsViewModel vm)
				{
					vm.StartRecording(setting);

					Dispatcher.InvokeAsync(() =>
					{
						try
						{
							var textBox = FindTextBoxForSetting(setting);
							if (textBox != null)
							{
								FocusManager.SetFocusedElement(this, textBox);
								textBox.Focus();
								Keyboard.Focus(textBox);
								textBox.CaretIndex = (textBox.Text ?? string.Empty).Length;
								textBox.ScrollToEnd();
							}
							else
							{
								FocusManager.SetFocusedElement(this, this);
								Focus();
							}
						}
						catch (Exception ex)
						{
							Services.AppDialog.Error("Ошибка установки фокуса записи: " + ex.Message, "Ошибка");
						}
					}, DispatcherPriority.Input);
				}
			}
			catch (Exception ex)
			{
				Services.AppDialog.Error("Ошибка начала записи горячей клавиши: " + ex.Message, "Ошибка");
			}
		}

		// Находит поле ввода, связанное с указанной настройкой горячей клавиши.
		private TextBox FindTextBoxForSetting(HotkeySetting setting)
		{
			var itemsControl = FindVisualChild<ItemsControl>(this);
			if (itemsControl == null) return null;

			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(itemsControl); i++)
			{
				var child = VisualTreeHelper.GetChild(itemsControl, i);
				var textBox = FindTextBoxInContainer(child, setting);
				if (textBox != null) return textBox;
			}
			return null;
		}

		// Выполняет поиск поля ввода горячей клавиши внутри контейнера.
		private TextBox FindTextBoxInContainer(DependencyObject container, HotkeySetting setting)
		{
			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(container); i++)
			{
				var child = VisualTreeHelper.GetChild(container, i);

				if (child is TextBox textBox && ReferenceEquals(textBox.Tag, setting))
					return textBox;

				var result = FindTextBoxInContainer(child, setting);
				if (result != null) return result;
			}
			return null;
		}

		// Находит первый визуальный дочерний элемент указанного типа.
		private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
		{
			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
			{
				var child = VisualTreeHelper.GetChild(parent, i);
				if (child is T result) return result;

				var childResult = FindVisualChild<T>(child);
				if (childResult != null) return childResult;
			}
			return null;
		}

		// Сохраняет текущие настройки приложения.
		private void Save_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				(DataContext as SettingsViewModel)?.Save();
			}
			catch (Exception ex)
			{
				Services.AppDialog.Error("Ошибка сохранения настроек: " + ex.Message, "Ошибка");
			}
		}

		// Сбрасывает настройки к значениям по умолчанию.
		private void Reset_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var vm = DataContext as SettingsViewModel;
				if (vm == null) return;

				vm.ResetToDefaults();
				SyncThemeButtons();
			}
			catch (Exception ex)
			{
				Services.AppDialog.Error("Ошибка сброса настроек: " + ex.Message, "Ошибка");
			}
		}

		// Перехватывает нажатия клавиш в режиме записи горячей клавиши.
		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			try
			{
				base.OnPreviewKeyDown(e);

				var vm = DataContext as SettingsViewModel;
				if (vm == null) return;

				if (vm.HandleKeyDown(e))
					e.Handled = true;
			}
			catch (Exception ex)
			{
				e.Handled = true;
				Services.AppDialog.Error("Ошибка обработки сочетания клавиш: " + ex.Message, "Ошибка");
			}
		}
	}
}