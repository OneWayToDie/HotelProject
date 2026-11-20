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
		// Пока выставляем галочку темы программно, не применяем её повторно.
		private bool _syncingTheme;

		public SettingsView()
		{
			InitializeComponent();
			Loaded += SettingsView_Loaded;
		}

		private void SettingsView_Loaded(object sender, RoutedEventArgs e)
		{
			SyncThemeButtons();
		}

		// Синхронизирует выбранную радиокнопку с темой из ViewModel.
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

		private void Theme_Checked(object sender, RoutedEventArgs e)
		{
			if (_syncingTheme) return;

			var vm = DataContext as SettingsViewModel;
			var radio = sender as RadioButton;
			if (vm == null || radio == null || radio.IsChecked != true) return;

			if (radio.Tag is string themeName)
				vm.SelectTheme(themeName);
		}

		private void RecordButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is Button btn && btn.Tag is HotkeySetting setting && DataContext is SettingsViewModel vm)
			{
				vm.StartRecording(setting);

				// Input выполняется уже после отрисовки (Render -> Loaded -> Input),
				// поэтому TextBox к этому моменту гарантированно существует в дереве.
				Dispatcher.InvokeAsync(() =>
				{
					var textBox = FindTextBoxForSetting(setting);
					if (textBox != null)
					{
						FocusManager.SetFocusedElement(this, textBox);
						// IsReadOnlyCaretVisible=True рисует каретку в read-only TextBox,
						// но только когда у него реальный клавиатурный фокус.
						textBox.Focus();
						Keyboard.Focus(textBox);
						textBox.CaretIndex = (textBox.Text ?? string.Empty).Length;
						textBox.ScrollToEnd();
					}
					else
					{
						// Фолбэк: если WPF упрямится и не находит TextBox, 
						// фокусируем сам UserControl. OnPreviewKeyDown всё равно перехватит клавиши!
						FocusManager.SetFocusedElement(this, this);
						Focus();
					}
				}, DispatcherPriority.Input);
			}
		}

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

		private TextBox FindTextBoxInContainer(DependencyObject container, HotkeySetting setting)
		{
			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(container); i++)
			{
				var child = VisualTreeHelper.GetChild(container, i);

				// Сравниваем ссылки на объекты (они должны быть идентичны благодаря {Binding})
				if (child is TextBox textBox && ReferenceEquals(textBox.Tag, setting))
					return textBox;

				var result = FindTextBoxInContainer(child, setting);
				if (result != null) return result;
			}
			return null;
		}

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

		private void Save_Click(object sender, RoutedEventArgs e)
			=> (DataContext as SettingsViewModel)?.Save();

		private void Reset_Click(object sender, RoutedEventArgs e)
		{
			var vm = DataContext as SettingsViewModel;
			if (vm == null) return;

			vm.ResetToDefaults();
			SyncThemeButtons();
		}

		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			base.OnPreviewKeyDown(e);

			var vm = DataContext as SettingsViewModel;
			if (vm == null) return;

			// Пока идёт запись, клавиши принадлежат настройкам: не даём им
			// нажимать кнопки, прокручивать список или закрывать диалог.
			if (vm.HandleKeyDown(e))
				e.Handled = true;
		}
	}
}