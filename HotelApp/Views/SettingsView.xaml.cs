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
		public SettingsView()
		{
			InitializeComponent();
		}

		private void RecordButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is Button btn && btn.Tag is HotkeySetting setting && DataContext is SettingsViewModel vm)
			{
				vm.StartRecording(setting);

				// Render гарантированно ждёт ПОЛНОЙ отрисовки визуального дерева
				Dispatcher.InvokeAsync(() =>
				{
					var textBox = FindTextBoxForSetting(setting);
					if (textBox != null)
					{
						// FocusManager надежнее, чем Keyboard.Focus, внутри DataTemplate
						FocusManager.SetFocusedElement(this, textBox);
						textBox.CaretIndex = 0;
					}
					else
					{
						// Фолбэк: если WPF упрямится и не находит TextBox, 
						// фокусируем сам UserControl. OnPreviewKeyDown всё равно перехватит клавиши!
						FocusManager.SetFocusedElement(this, this);
					}
				}, DispatcherPriority.Render);
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
			=> (DataContext as SettingsViewModel)?.ResetToDefaults();

		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			base.OnPreviewKeyDown(e);
			(DataContext as SettingsViewModel)?.HandleKeyDown(e);
		}
	}
}