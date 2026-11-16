using System;
using System.Windows;
using System.Windows.Controls;
using HotelApp.ViewModels;
using HotelApp.Views;
using HotelApp.Services;
using MaterialDesignThemes.Wpf;

namespace HotelApp
{
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
		}

		private void MenuList_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (DataContext is MainViewModel vm && MenuList != null)
			{
				if (MenuList.SelectedIndex == 0) vm.CurrentView = new RoomsViewModel(vm.HotelPtr, vm);
				else if (MenuList.SelectedIndex == 1) vm.CurrentView = new TextBlock { Text = "База гостей (в разработке)", Margin = new Thickness(20), FontSize = 20 };
				else if (MenuList.SelectedIndex == 2) vm.CurrentView = new TextBlock { Text = "Аналитика (в разработке)", Margin = new Thickness(20), FontSize = 20 };
			}
		}

		// --- ДОБАВЛЕНИЕ КОРПУСА ---
		private async void OpenAddBuilding_Click(object sender, RoutedEventArgs e)
		{
			if (!(DataContext is MainViewModel vm)) return;
			var template = (DataTemplate)FindResource("AddBuildingDialogTemplate");
			var dialogContent = template.LoadContent() as FrameworkElement;
			await DialogHost.Show(dialogContent, "RootDialog");
		}

		// --- ДОБАВЛЕНИЕ НОМЕРА ---
		private async void OpenAddRoom_Click(object sender, RoutedEventArgs e)
		{
			if (!(DataContext is MainViewModel vm)) return;
			var template = (DataTemplate)FindResource("AddRoomDialogTemplate");
			var dialogContent = template.LoadContent() as FrameworkElement;
			await DialogHost.Show(dialogContent, "RootDialog");
		}

		// --- ПОДТВЕРЖДЕНИЕ ДОБАВЛЕНИЯ КОРПУСА ---
		private void ConfirmAddBuilding_Click(object sender, RoutedEventArgs e)
		{
			if (DataContext is MainViewModel vm)
			{
				var button = sender as Button;
				var stackPanel = button.Parent as StackPanel;
				var mainStackPanel = stackPanel.Parent as StackPanel;
				var textBox = mainStackPanel.Children[1] as TextBox;

				if (textBox != null && !string.IsNullOrWhiteSpace(textBox.Text))
				{
					NativeMethods.Hotel_AddBuilding(vm.HotelPtr, textBox.Text);
					if (vm.CurrentView is RoomsViewModel roomsVm)
					{
						roomsVm.RefreshData();
						vm.AddLog($"Добавлен корпус: {textBox.Text}");
					}
					vm.SaveData(); // <--- СОХРАНЯЕМ В ФАЙЛ
				}
				DialogHost.CloseDialogCommand.Execute(null, null);
			}
		}

		// --- ПОДТВЕРЖДЕНИЕ ДОБАВЛЕНИЯ НОМЕРА ---
		private void ConfirmAddRoom_Click(object sender, RoutedEventArgs e)
		{
			if (DataContext is MainViewModel vm)
			{
				var button = sender as Button;
				var stackPanel = button.Parent as StackPanel;
				var mainStackPanel = stackPanel.Parent as StackPanel;

				var buildingCombo = mainStackPanel.Children[2] as ComboBox;
				var typeCombo = mainStackPanel.Children[4] as ComboBox;
				var numberBox = mainStackPanel.Children[5] as TextBox;
				var priceBox = mainStackPanel.Children[6] as TextBox;

				if (buildingCombo?.SelectedValue != null && !string.IsNullOrWhiteSpace(numberBox?.Text))
				{
					int buildingId = (int)buildingCombo.SelectedValue;
					int roomType = typeCombo.SelectedIndex;
					string roomNumber = numberBox.Text;

					double price = 2000;
					double.TryParse(priceBox.Text, out price);

					NativeMethods.Hotel_AddRoom(vm.HotelPtr, buildingId, roomType, roomNumber, price);

					if (vm.CurrentView is RoomsViewModel roomsVm)
					{
						roomsVm.RefreshData();
						vm.AddLog($"Добавлен номер {roomNumber} за {price} ₽");
					}
					vm.SaveData(); // <--- СОХРАНЯЕМ В ФАЙЛ
				}
				else
				{
					MessageBox.Show("Заполните все поля!");
				}

				DialogHost.CloseDialogCommand.Execute(null, null);
			}
		}
	}
}