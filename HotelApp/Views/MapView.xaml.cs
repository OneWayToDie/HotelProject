using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HotelApp.Models;
using HotelApp.Services;
using HotelApp.ViewModels;
using MaterialDesignThemes.Wpf;
using Microsoft.Xaml.Behaviors.Layout;

namespace HotelApp.Views
{
	public partial class MapView : UserControl
	{
		public MapView()
		{
			InitializeComponent();
		}

		private async void Room_Click(object sender, MouseButtonEventArgs e)
		{
			var border = sender as Border;
			var room = border.DataContext as RoomModel;
			if (room == null) return;

			if (room.Status != 0) // Если не свободен
			{
				MessageBox.Show($"Номер {room.Number} занят или на уборке!\nГость: {room.DisplayGuest}", "Информация");
				return;
			}

			// Открываем окно заселения (то же, что и в таблице)
			var template = (DataTemplate)Application.Current.MainWindow.FindResource("CheckInDialogTemplate");
			var dialogContent = template.LoadContent() as FrameworkElement;
			var result = await DialogHost.Show(dialogContent, "RootDialog");

			if (result != null && result.ToString() == "True")
			{
				var card = dialogContent as MaterialDesignThemes.Wpf.Card;
				var panel = card.Content as StackPanel;

				string guestName = (panel.Children[1] as TextBox).Text;
				string passport = (panel.Children[2] as TextBox).Text;
				var dateIn = (panel.Children[4] as DatePicker).SelectedDate;
				var dateOut = (panel.Children[6] as DatePicker).SelectedDate;

				if (!string.IsNullOrWhiteSpace(guestName) && dateIn.HasValue && dateOut.HasValue)
				{
					int days = (dateOut.Value - dateIn.Value).Days;
					if (days <= 0) { MessageBox.Show("Дата выезда должна быть позже заезда!"); return; }

					var mainVm = Application.Current.MainWindow.DataContext as MainViewModel;
					if (mainVm != null)
					{
						NativeMethods.Hotel_CheckIn(mainVm.HotelPtr, room.Id, guestName, passport,
							dateIn.Value.ToString("yyyy-MM-dd"), dateOut.Value.ToString("yyyy-MM-dd"), days);

						mainVm.SaveData();
						mainVm.AddLog($"Заселение (Карта): {guestName} -> {room.Number}", LogType.Success);

						// Обновляем карту
						var mapVm = DataContext as MapViewModel;
						mapVm?.Refresh(mainVm.HotelPtr);
						mainVm.UpdateStats();
					}
				}
			}
		}
	}
}