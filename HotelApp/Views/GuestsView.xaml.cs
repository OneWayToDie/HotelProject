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

		// Инициализирует представление списка гостей.
		public GuestsView()
		{
			InitializeComponent();
			GuestsGrid.AddHandler(DataGridRow.PreviewMouseLeftButtonDownEvent,
				new MouseButtonEventHandler(DataGridRow_PreviewMouseLeftButtonDown), true);
		}

		// Снимает выделение строки гостя при повторном щелчке по ней.
		private void DataGridRow_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			var row = FindVisualParent<DataGridRow>(e.OriginalSource as DependencyObject);
			if (row == null) return;

			var guest = row.DataContext as GuestModel;
			if (guest == null) return;

			if (GuestsGrid.SelectedItem == guest && _lastSelectedGuest == guest)
			{
				e.Handled = true;
				GuestsGrid.SelectedItem = null;
				_lastSelectedGuest = null;
			}
			else
			{
				_lastSelectedGuest = guest;
			}
		}

		// Находит родительский элемент указанного типа в визуальном дереве.
		private static T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
		{
			while (child != null)
			{
				if (child is T parent) return parent;
				child = VisualTreeHelper.GetParent(child);
			}
			return null;
		}

		// Обрабатывает удаление выбранного гостя.
		private void DeleteGuest_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (!(DataContext is GuestsViewModel vm)) return;
				var guest = GuestsGrid.SelectedItem as GuestModel ?? (sender as FrameworkElement)?.Tag as GuestModel;
				if (guest == null)
				{
					AppDialog.Warning("Выберите гостя для удаления.", "База гостей");
					return;
				}

				if (!AppDialog.Confirm($"Удалить гостя {guest.FullName}?", "Удаление гостя", "Удалить", "Отмена"))
					return;

				vm.DeleteGuest(guest);
			}
			catch (Exception ex)
			{
				AppDialog.Error("Не удалось удалить гостя: " + ex.Message, "Ошибка");
			}
		}
	}
}