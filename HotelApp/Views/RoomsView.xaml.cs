using System;
using System.Windows;
using System.Windows.Controls;
using HotelApp.Models;
using HotelApp.ViewModels;
using MaterialDesignThemes.Wpf;

namespace HotelApp.Views
{
	public partial class RoomsView : UserControl
	{
		public RoomsView()
		{
			InitializeComponent();
		}

		// --- ФИЛЬТРЫ ---
		private void Filter_Checked(object sender, RoutedEventArgs e)
		{
			if (DataContext is RoomsViewModel vm && sender is RadioButton rb)
			{
				if (int.TryParse(rb.Tag.ToString(), out int filterIndex))
				{
					vm.CurrentFilter = filterIndex;
					vm.RefreshData();
				}
			}
		}

		// --- ЗАСЕЛЕНИЕ (без изменений, только добавили лог) ---
		private async void CheckIn_Click(object sender, RoutedEventArgs e)
		{
			var selectedRoom = RoomsGrid.SelectedItem as RoomModel;
			if (selectedRoom == null || selectedRoom.Status != 0) return;

			var template = (DataTemplate)Application.Current.MainWindow.FindResource("CheckInDialogTemplate");
			var dialogContent = template.LoadContent() as FrameworkElement;
			var result = await DialogHost.Show(dialogContent, "RootDialog");

			if (result != null && result.ToString() == "True")
			{
				var card = dialogContent as MaterialDesignThemes.Wpf.Card;
				var panel = card.Content as StackPanel;

				string guestName = (panel.Children[1] as TextBox).Text;
				string passport = (panel.Children[2] as TextBox).Text;

				// Достаем даты из DatePicker'ов
				var dateIn = (panel.Children[4] as DatePicker).SelectedDate;
				var dateOut = (panel.Children[6] as DatePicker).SelectedDate;

				if (!string.IsNullOrWhiteSpace(guestName) && dateIn.HasValue && dateOut.HasValue)
				{
					// Считаем разницу дней в C#
					int days = (dateOut.Value - dateIn.Value).Days;
					if (days <= 0) { MessageBox.Show("Дата выезда должна быть позже заезда!"); return; }

					if (DataContext is RoomsViewModel vm)
					{
						vm.CheckIn(selectedRoom.Id, guestName, passport, dateIn.Value.ToString("yyyy-MM-dd"), dateOut.Value.ToString("yyyy-MM-dd"), days);
						if (Application.Current.MainWindow.DataContext is MainViewModel mainVm)
							mainVm.AddLog($"Заселение: {guestName} -> Номер {selectedRoom.Number} ({days} дн.)");
					}
				}
			}
		}

		private async void EditPrice_Click(object sender, RoutedEventArgs e)
		{
			var selectedRoom = RoomsGrid.SelectedItem as RoomModel;
			if (selectedRoom == null) { MessageBox.Show("Выберите номер!"); return; }

			var template = (DataTemplate)Application.Current.MainWindow.FindResource("EditPriceDialogTemplate");
			var dialogContent = template.LoadContent() as FrameworkElement;
			var result = await DialogHost.Show(dialogContent, "RootDialog");

			if (result != null && result.ToString() == "True")
			{
				var card = dialogContent as MaterialDesignThemes.Wpf.Card;
				var panel = card.Content as StackPanel;
				var textBox = panel.Children[1] as TextBox;

				if (textBox != null && double.TryParse(textBox.Text, out double newPrice))
				{
					if (DataContext is RoomsViewModel vm)
					{
						vm.UpdatePrice(selectedRoom.Id, newPrice);

						if (Application.Current.MainWindow.DataContext is MainViewModel mainVm)
							mainVm.AddLog($"Номер {selectedRoom.Number}: новая цена {newPrice} ₽.");
					}
				}
				else
				{
					MessageBox.Show("Не удалось распознать цену! Введите просто число, например: 3000");
				}
			}
		}

		// --- ВЫСЕЛЕНИЕ (добавили лог) ---
		private void CheckOut_Click(object sender, RoutedEventArgs e)
		{
			var selectedRoom = RoomsGrid.SelectedItem as RoomModel;
			if (selectedRoom == null) { MessageBox.Show("Выберите номер!"); return; }
			if (selectedRoom.Status != 1) { MessageBox.Show("Номер пуст!"); return; }

			if (DataContext is RoomsViewModel vm)
			{
				vm.CheckOut(selectedRoom.Id);
				if (Application.Current.MainWindow.DataContext is MainViewModel mainVm)
					mainVm.AddLog($"Выселение: Гость из номера {selectedRoom.Number} выехал.");
			}
		}

		// --- ЗАВЕРШЕНИЕ УБОРКИ ---
		private void FinishCleaning_Click(object sender, RoutedEventArgs e)
		{
			var selectedRoom = RoomsGrid.SelectedItem as RoomModel;
			if (selectedRoom == null) { MessageBox.Show("Выберите номер!"); return; }

			if (selectedRoom.Status != 2) // 2 = На уборке
			{
				MessageBox.Show("Этот номер не находится на уборке!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (DataContext is RoomsViewModel vm)
			{
				vm.FinishCleaning(selectedRoom.Id);
				if (Application.Current.MainWindow.DataContext is MainViewModel mainVm)
					mainVm.AddLog($"Уборка: Номер {selectedRoom.Number} готов к заселению.");
			}
		}
	}
}