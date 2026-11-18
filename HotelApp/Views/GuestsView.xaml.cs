using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using HotelApp.Models;
using HotelApp.Services;
using HotelApp.ViewModels;

namespace HotelApp.Views
{
	public partial class GuestsView : UserControl
	{
		private GuestModel _lastSelectedGuest;

		public GuestsView()
		{
			InitializeComponent();

			// Подписываемся на клик по строкам DataGrid
			GuestsGrid.AddHandler(DataGridRow.PreviewMouseLeftButtonDownEvent,
				new MouseButtonEventHandler(DataGridRow_PreviewMouseLeftButtonDown), true);
		}

		private void DataGridRow_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			// Получаем строку, по которой кликнули
			var row = FindVisualParent<DataGridRow>(e.OriginalSource as DependencyObject);
			if (row == null) return;

			var guest = row.DataContext as GuestModel;
			if (guest == null) return;

			// Если кликнули по уже выбранной строке — снимаем выделение
			if (_lastSelectedGuest == guest && GuestsGrid.SelectedItem == guest)
			{
				GuestsGrid.SelectedItem = null;
				_lastSelectedGuest = null;
				e.Handled = true; // Предотвращаем дальнейшую обработку
			}
			else
			{
				_lastSelectedGuest = guest;
			}
		}

		// Вспомогательный метод для поиска родительского элемента
		private T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
		{
			DependencyObject parentObject = VisualTreeHelper.GetParent(child);
			if (parentObject == null) return null;

			T parent = parentObject as T;
			if (parent != null)
				return parent;

			return FindVisualParent<T>(parentObject);
		}

		// --- УДАЛЕНИЕ ГОСТЯ ИЗ БАЗЫ ---
		private void DeleteGuest_Click(object sender, RoutedEventArgs e)
		{
			if (!(DataContext is GuestsViewModel vm)) return;
			try
			{
				if (!(GuestsGrid.SelectedItem is GuestModel guest))
				{
					AppDialog.Warning("Выберите гостя в таблице.", "Удаление гостя");
					return;
				}
				bool confirmed = AppDialog.Confirm(
					$"Удалить гостя «{guest.FullName}» из базы?\nЗаписи о бронированиях сохранятся.",
					"Удаление гостя", "Удалить", "Отмена");
				if (!confirmed) return;
				vm.DeleteGuest(guest);
			}
			catch (Exception ex)
			{
				if (Application.Current.MainWindow?.DataContext is MainViewModel mainVm)
					mainVm.AddLog("Удаление гостя: " + ex.Message, LogType.Error);
				AppDialog.Error("Не удалось удалить гостя.\n" + ex.Message, "База гостей");
			}
		}
	}
}