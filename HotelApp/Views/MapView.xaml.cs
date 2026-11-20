using System;
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
	public partial class MapView : UserControl
	{
		// Инициализирует представление карты отеля.
		public MapView()
		{
			InitializeComponent();
		}

		// Обрабатывает щелчок по номеру на карте отеля.
		private async void Room_Click(object sender, MouseButtonEventArgs e)
		{
			try
			{
				await RoomClickAsync(sender);
			}
			catch (Exception ex)
			{
				ReportError("заселение с карты", ex);
			}
		}

		// Выполняет сценарий заселения при выборе свободного номера на карте.
		private async Task RoomClickAsync(object sender)
		{
			var border = sender as Border;
			var room = border?.DataContext as RoomModel;
			var mainVm = Window.GetWindow(this)?.DataContext as MainViewModel;
			if (room == null || mainVm == null) return;

			if (room.Status != 0)
			{
				AppDialog.Info($"Номер {room.Number} сейчас недоступен для заселения.\nГость: {room.DisplayGuest}", "Карта отеля");
				return;
			}

			var template = Application.Current.MainWindow?.FindResource("CheckInDialogTemplate") as DataTemplate;
			var dialogContent = template?.LoadContent() as FrameworkElement;
			if (dialogContent == null) return;

			var result = await DialogHost.Show(dialogContent, "RootDialog");
			if (result == null || result.ToString() != "True") return;

			var card = dialogContent as Card;
			var panel = card?.Content as StackPanel;
			var nameBox = panel?.Children.Count > 1 ? panel.Children[1] as TextBox : dialogContent.FindName("GuestNameTextBox") as TextBox;
			var passportBox = panel?.Children.Count > 2 ? panel.Children[2] as TextBox : dialogContent.FindName("GuestPassportTextBox") as TextBox;
			var inPicker = panel?.Children.Count > 4 ? panel.Children[4] as DatePicker : dialogContent.FindName("CheckInDatePicker") as DatePicker;
			var outPicker = panel?.Children.Count > 6 ? panel.Children[6] as DatePicker : dialogContent.FindName("CheckOutDatePicker") as DatePicker;

			string guestName = InputRules.NormalizeFullName(nameBox?.Text);
			string passport = passportBox?.Text?.Trim() ?? string.Empty;
			if (string.IsNullOrWhiteSpace(guestName) || string.IsNullOrWhiteSpace(passport) ||
				inPicker?.SelectedDate == null || outPicker?.SelectedDate == null)
			{
				AppDialog.Warning("Заполните все поля для заселения.", "Карта отеля");
				return;
			}

			DateTime checkIn = inPicker.SelectedDate.Value.Date;
			DateTime checkOut = outPicker.SelectedDate.Value.Date;
			if (checkOut <= checkIn)
			{
				AppDialog.Warning("Дата выезда должна быть позже даты заезда.", "Карта отеля");
				return;
			}

			int days = (checkOut - checkIn).Days;
			if (days <= 0) days = 1;
			string inStr = checkIn.ToString("yyyy-MM-dd");
			string outStr = checkOut.ToString("yyyy-MM-dd");

			var roomsVm = new RoomsViewModel(mainVm.HotelPtr, mainVm);
			roomsVm.CheckIn(room.Id, guestName, passport, inStr, outStr, days);

			(DataContext as MapViewModel)?.Refresh(mainVm.HotelPtr);
			mainVm.UpdateStats();
		}

		// Показывает ошибку операции и записывает её в журнал.
		private void ReportError(string operation, Exception ex)
		{
			var mainVm = Window.GetWindow(this)?.DataContext as MainViewModel;
			string message = string.IsNullOrWhiteSpace(ex?.Message)
				? $"Не удалось выполнить операцию: {operation}."
				: ex.Message;

			mainVm?.AddLog($"Ошибка ({operation}): {message}", LogType.Error);
			AppDialog.Error(message, "Ошибка");
		}
	}
}