using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HotelApp.Models;
using HotelApp.Services;
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
			try
			{
				await CheckInAsync();
			}
			catch (Exception ex)
			{
				ReportError("заселение гостя", ex);
			}
		}

		private async void RoomsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			try
			{
				var row = ItemsControl.ContainerFromElement(RoomsGrid, e.OriginalSource as DependencyObject) as DataGridRow;
				if (row == null) return;

				var room = row.Item as RoomModel;
				if (room == null) return;

				// Заселение по двойному клику — только для номеров, которые уже можно заселить сейчас.
				if (room.Status != 0 || !room.IsWaitingForCheckIn) return;

				RoomsGrid.SelectedItem = room;
				await CheckInAsync();
			}
			catch (Exception ex)
			{
				ReportError("заселение гостя по двойному клику", ex);
			}
		}

		private async Task CheckInAsync()
		{
			var selectedRoom = RoomsGrid.SelectedItem as RoomModel;
			if (selectedRoom == null || selectedRoom.Status != 0)
			{
				AppDialog.Warning("Выберите свободный номер!", "Заселение");
				return;
			}

			var template = (DataTemplate)Application.Current.MainWindow.FindResource("CheckInDialogTemplate");
			var dialogContent = template.LoadContent() as FrameworkElement;
			var card = dialogContent as MaterialDesignThemes.Wpf.Card;
			var panel = card?.Content as StackPanel;

			// --- НОВОЕ: Ищем активную бронь на сегодня и подставляем данные ---
			if (DataContext is RoomsViewModel vm && panel != null)
			{
				string todayStr = DateTime.Today.ToString("yyyy-MM-dd");
				var activeBooking = vm.Bookings.FirstOrDefault(b => b.RoomId == selectedRoom.Id
					&& string.Compare(b.CheckInDate, todayStr) <= 0
					&& string.Compare(b.CheckOutDate, todayStr) > 0);

				if (activeBooking != null)
				{
					var nameBox = panel.Children[1] as TextBox;
					var passportBox = panel.Children[2] as TextBox;
					var inPicker = panel.Children[4] as DatePicker;
					var outPicker = panel.Children[6] as DatePicker;

					if (nameBox != null) nameBox.Text = activeBooking.GuestName;
					if (passportBox != null) passportBox.Text = InputRules.FormatPassportDigits(activeBooking.Passport);

					// Дата заезда - сегодня, дата выезда - из брони
					if (inPicker != null) inPicker.SelectedDate = DateTime.Today;
					if (outPicker != null && DateTime.TryParse(activeBooking.CheckOutDate, out var outDate))
						outPicker.SelectedDate = outDate;
				}
			}
			// -----------------------------------------------------------------

			var result = await DialogHost.Show(dialogContent, "RootDialog");

			if (result != null && result.ToString() == "True")
			{
				var nameBox = panel?.Children[1] as TextBox;
				var passportBox = panel?.Children[2] as TextBox;
				var inPicker = panel?.Children[4] as DatePicker;
				var outPicker = panel?.Children[6] as DatePicker;

				string guestName = nameBox?.Text;
				string passport = passportBox?.Text;
				var dateIn = inPicker?.SelectedDate;
				var dateOut = outPicker?.SelectedDate;

				if (!string.IsNullOrWhiteSpace(guestName) && dateIn.HasValue && dateOut.HasValue)
				{
					int days = (dateOut.Value - dateIn.Value).Days;
					if (days <= 0)
					{
						AppDialog.Warning("Дата выезда должна быть позже заезда!", "Заселение");
						return;
					}

					if (DataContext is RoomsViewModel roomsVm)
					{
						roomsVm.CheckIn(selectedRoom.Id, guestName, passport ?? string.Empty,
							dateIn.Value.ToString("yyyy-MM-dd"), dateOut.Value.ToString("yyyy-MM-dd"), days);
					}
				}
			}
		}

		// --- БРОНИРОВАНИЕ ---
		private async void Book_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				await BookAsync();
			}
			catch (Exception ex)
			{
				ReportError("бронирование номера", ex);
			}
		}

		private async Task BookAsync()
		{
			var selectedRoom = RoomsGrid.SelectedItem as RoomModel;
			if (selectedRoom == null) { AppDialog.Warning("Выберите номер!", "Бронирование"); return; }

			var template = (DataTemplate)Application.Current.MainWindow.FindResource("BookingDialogTemplate");
			var dialogContent = template.LoadContent() as FrameworkElement;
			var result = await DialogHost.Show(dialogContent, "RootDialog");

			if (result != null && result.ToString() == "True")
			{
				var card = dialogContent as MaterialDesignThemes.Wpf.Card;
				var panel = card?.Content as StackPanel;
				if (panel == null) return;

				string guestName = (panel.Children[1] as TextBox)?.Text;
				string passport = (panel.Children[2] as TextBox)?.Text;
				var dateIn = (panel.Children[4] as DatePicker)?.SelectedDate;
				var dateOut = (panel.Children[6] as DatePicker)?.SelectedDate;

				if (!string.IsNullOrWhiteSpace(guestName) && dateIn.HasValue && dateOut.HasValue && DataContext is RoomsViewModel vm)
				{
					vm.BookRoom(selectedRoom.Id, guestName, passport ?? string.Empty,
						dateIn.Value.ToString("yyyy-MM-dd"), dateOut.Value.ToString("yyyy-MM-dd"));
				}
			}
		}

		private async void EditPrice_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				await EditPriceAsync();
			}
			catch (Exception ex)
			{
				ReportError("изменение цены", ex);
			}
		}

		private async Task EditPriceAsync()
		{
			var selectedRoom = RoomsGrid.SelectedItem as RoomModel;
			if (selectedRoom == null) { AppDialog.Warning("Выберите номер!", "Изменение цены"); return; }

			var template = (DataTemplate)Application.Current.MainWindow.FindResource("EditPriceDialogTemplate");
			var dialogContent = template.LoadContent() as FrameworkElement;
			var result = await DialogHost.Show(dialogContent, "RootDialog");

			if (result != null && result.ToString() == "True")
			{
				var card = dialogContent as MaterialDesignThemes.Wpf.Card;
				var panel = card?.Content as StackPanel;
				var textBox = panel?.Children[1] as TextBox;

				if (textBox != null && InputRules.IsValidPrice(textBox.Text, out int newPrice))
				{
					if (DataContext is RoomsViewModel vm)
					{
						// Обновление цены и логирование выполняет ViewModel
						vm.UpdatePrice(selectedRoom.Id, newPrice);
					}
				}
				else
				{
					if (Application.Current.MainWindow.DataContext is MainViewModel mainVm)
						mainVm.AddLog($"Номер {selectedRoom.Number}: цена не изменена (некорректный ввод).", LogType.Error);

					AppDialog.Warning("Не удалось распознать цену! Введите целое число, например: 3000", "Изменение цены");
				}
			}
		}

		// --- ВЫСЕЛЕНИЕ (добавили лог) ---
		private void CheckOut_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				CheckOutCore();
			}
			catch (Exception ex)
			{
				ReportError("выселение гостя", ex);
			}
		}

		private void CheckOutCore()
		{
			var selectedRoom = RoomsGrid.SelectedItem as RoomModel;
			if (selectedRoom == null) { AppDialog.Warning("Выберите номер!", "Выселение"); return; }
			if (selectedRoom.Status != 1) { AppDialog.Warning("Номер пуст!", "Выселение"); return; }

			if (DataContext is RoomsViewModel vm)
			{
				// Логирование и обновление данных выполняет ViewModel
				vm.CheckOut(selectedRoom.Id);
			}
		}

		// --- ЗАВЕРШЕНИЕ УБОРКИ ---
		private void FinishCleaning_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				FinishCleaningCore();
			}
			catch (Exception ex)
			{
				ReportError("завершение уборки", ex);
			}
		}

		private void FinishCleaningCore()
		{
			var selectedRoom = RoomsGrid.SelectedItem as RoomModel;
			if (selectedRoom == null) { AppDialog.Warning("Выберите номер!", "Уборка"); return; }

			if (selectedRoom.Status != 2) // 2 = На уборке
			{
				AppDialog.Warning("Этот номер не находится на уборке!", "Ошибка");
				return;
			}

			if (DataContext is RoomsViewModel vm)
			{
				// Логирование и обновление данных выполняет ViewModel
				vm.FinishCleaning(selectedRoom.Id);
			}
		}

		// Единая обработка ошибок экрана «Номера и Фонд»
		private void ReportError(string action, Exception ex)
		{
			if (Application.Current.MainWindow?.DataContext is MainViewModel mainVm)
				mainVm.AddLog($"{action}: {ex.Message}", LogType.Error);

			AppDialog.Error($"Не удалось выполнить действие «{action}».\n{ex.Message}", "Номера и Фонд");
		}
	}
}