using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HotelApp.Models;
using HotelApp.Services;
using HotelApp.ViewModels;
// AppDialog находится в HotelApp.Services
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
			try
			{
				await RoomClickAsync(sender);
			}
			catch (Exception ex)
			{
				ReportError("заселение с карты", ex);
			}
		}

		private async Task RoomClickAsync(object sender)
		{
			var border = sender as Border;
			var room = border.DataContext as RoomModel;
			if (room == null) return;

			if (room.Status != 0) // Если не свободен
			{
				AppDialog.Info($"Номер {room.Number} занят или на уборке!\nГость: {room.DisplayGuest}", "Информация");
				return;
			}

			// Открываем окно заселения (то же, что и в таблице)
			var template = (DataTemplate)Application.Current.MainWindow.FindResource("CheckInDialogTemplate");
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

				if (!string.IsNullOrWhiteSpace(guestName) && dateIn.HasValue && dateOut.HasValue)
				{
					int days = (dateOut.Value - dateIn.Value).Days;
					if (days <= 0) { AppDialog.Warning("Дата выезда должна быть позже заезда!", "Заселение"); return; }

					var mainVm = Application.Current.MainWindow.DataContext as MainViewModel;
					if (mainVm != null)
					{
						string inDate = dateIn.Value.ToString("yyyy-MM-dd");
						string outDate = dateOut.Value.ToString("yyyy-MM-dd");

						// Проверяем чужую бронь на выбранные даты
						if (mainVm.HasBookingConflict(room.Id, inDate, outDate, guestName, passport))
						{
							AppDialog.Warning($"Номер {room.Number} забронирован на выбранные даты!\nВыберите другие даты или снимите бронь.",
								"Бронирование");
							return;
						}

						bool success = NativeMethods.Hotel_CheckIn(mainVm.HotelPtr, room.Id, guestName, passport ?? string.Empty,
							inDate, outDate, days);

						mainVm.SaveData();

						if (success)
						{
							mainVm.AddLog($"Заселение (Карта): {guestName} -> {room.Number}", LogType.Success);
						}
						else
						{
							mainVm.AddLog($"Заселение (Карта) не выполнено: номер {room.Number} занят или на уборке.", LogType.Error);
							AppDialog.Warning($"Не удалось заселить гостя в номер {room.Number}.", "Ошибка");
						}

						// Обновляем карту
						var mapVm = DataContext as MapViewModel;
						mapVm?.Refresh(mainVm.HotelPtr);
						mainVm.UpdateStats();
					}
				}
			}
		}

		// Единая обработка ошибок экрана «Карта отеля»
		private void ReportError(string action, Exception ex)
		{
			if (Application.Current.MainWindow?.DataContext is MainViewModel mainVm)
				mainVm.AddLog($"{action}: {ex.Message}", LogType.Error);

			AppDialog.Error($"Не удалось выполнить действие «{action}».\n{ex.Message}", "Карта отеля");
		}
	}
}