using System;
using System.Windows;
using System.Windows.Controls;
using HotelApp.ViewModels;
using HotelApp.Views;
using HotelApp.Services;
using HotelApp.Models;
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
				if (MenuList.SelectedIndex == 0) // Номера и Фонд
					vm.CurrentView = new RoomsViewModel(vm.HotelPtr, vm);

				else if (MenuList.SelectedIndex == 1) // База гостей
					vm.CurrentView = new GuestsView { DataContext = new GuestsViewModel(vm.HotelPtr) };

				else if (MenuList.SelectedIndex == 2) // Аналитика
					vm.CurrentView = new AnalyticsView { DataContext = new AnalyticsViewModel(vm.HotelPtr) };

				else if (MenuList.SelectedIndex == 3) // Карта отеля
					vm.CurrentView = new MapView { DataContext = new MapViewModel(vm.HotelPtr) };
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

				if (textBox == null || string.IsNullOrWhiteSpace(textBox.Text))
				{
					MessageBox.Show("Введите название корпуса!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
					return; // Окно не закрываем
				}

				NativeMethods.Hotel_AddBuilding(vm.HotelPtr, textBox.Text.Trim());
				vm.RefreshBuildings();
				if (vm.CurrentView is RoomsViewModel roomsVm)
				{
					roomsVm.RefreshData();
				}
				vm.AddLog($"Добавлен корпус: {textBox.Text.Trim()}", LogType.Success);
				vm.SaveData(); // <--- СОХРАНЯЕМ В ФАЙЛ

				DialogHost.CloseDialogCommand.Execute(null, button);
			}
		}

		// --- ПОДТВЕРЖДЕНИЕ ДОБАВЛЕНИЯ НОМЕРА ---
		private void ConfirmAddRoom_Click(object sender, RoutedEventArgs e)
		{
			if (!(DataContext is MainViewModel vm)) return;
			if (!(sender is Button button)) return;

			var stackPanel = button.Parent as StackPanel;
			var mainStackPanel = stackPanel?.Parent as StackPanel;
			if (mainStackPanel == null) return;

			var buildingCombo = mainStackPanel.Children[2] as ComboBox;
			var typeCombo = mainStackPanel.Children[4] as ComboBox;
			var numberBox = mainStackPanel.Children[5] as TextBox;
			var priceBox = mainStackPanel.Children[6] as TextBox;

			// Проверка на пустоту: без корпуса и номера комнаты ничего не создаем
			if (buildingCombo?.SelectedValue == null)
			{
				MessageBox.Show("Выберите корпус!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return; // Окно не закрываем
			}

			if (string.IsNullOrWhiteSpace(numberBox?.Text))
			{
				MessageBox.Show("Введите номер комнаты!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return; // Окно не закрываем
			}

			if (!double.TryParse(priceBox?.Text, out double price) || price <= 0)
			{
				MessageBox.Show("Введите корректную цену (больше нуля)!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return; // Окно не закрываем
			}

			int buildingId = (int)buildingCombo.SelectedValue;
			int roomType = typeCombo?.SelectedIndex ?? 0;
			string roomNumber = numberBox.Text.Trim();

			NativeMethods.Hotel_AddRoom(vm.HotelPtr, buildingId, roomType, roomNumber, price);

			if (vm.CurrentView is RoomsViewModel roomsVm)
			{
				roomsVm.RefreshData();
			}
			vm.AddLog($"Добавлен номер {roomNumber} за {price:N0} ₽", LogType.Success);
			vm.SaveData(); // <--- СОХРАНЯЕМ В ФАЙЛ

			DialogHost.CloseDialogCommand.Execute(null, button);
		}

		// --- ПОДТВЕРЖДЕНИЕ ЗАСЕЛЕНИЯ (с валидацией дат) ---
		private void ConfirmCheckIn_Click(object sender, RoutedEventArgs e)
		{
			if (!(sender is Button button)) return;

			var buttonsPanel = button.Parent as StackPanel;
			var mainPanel = buttonsPanel?.Parent as StackPanel;
			if (mainPanel == null) return;

			var nameBox = mainPanel.Children[1] as TextBox;
			var inPicker = mainPanel.Children[4] as DatePicker;
			var outPicker = mainPanel.Children[6] as DatePicker;

			if (string.IsNullOrWhiteSpace(nameBox?.Text))
			{
				MessageBox.Show("Введите ФИО гостя!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return; // Окно не закрываем
			}

			if (!inPicker.SelectedDate.HasValue || !outPicker.SelectedDate.HasValue)
			{
				MessageBox.Show("Выберите дату заезда и дату выезда!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return; // Окно не закрываем
			}

			if (outPicker.SelectedDate.Value.Date <= inPicker.SelectedDate.Value.Date)
			{
				MessageBox.Show("Дата выезда должна быть позже даты заезда!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return; // Окно не закрываем
			}

			DialogHost.CloseDialogCommand.Execute("True", button);
		}

		// --- ПОДТВЕРЖДЕНИЕ БРОНИРОВАНИЯ (с валидацией дат) ---
		private void ConfirmBooking_Click(object sender, RoutedEventArgs e)
		{
			if (!(sender is Button button)) return;

			var buttonsPanel = button.Parent as StackPanel;
			var mainPanel = buttonsPanel?.Parent as StackPanel;
			if (mainPanel == null) return;

			var nameBox = mainPanel.Children[1] as TextBox;
			var inPicker = mainPanel.Children[4] as DatePicker;
			var outPicker = mainPanel.Children[6] as DatePicker;

			if (string.IsNullOrWhiteSpace(nameBox?.Text))
			{
				MessageBox.Show("Введите ФИО гостя!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return; // Окно не закрываем
			}

			if (!inPicker.SelectedDate.HasValue || !outPicker.SelectedDate.HasValue)
			{
				MessageBox.Show("Выберите дату заезда и дату выезда!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return; // Окно не закрываем
			}

			if (outPicker.SelectedDate.Value.Date <= inPicker.SelectedDate.Value.Date)
			{
				MessageBox.Show("Дата выезда должна быть позже даты заезда!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return; // Окно не закрываем
			}

			DialogHost.CloseDialogCommand.Execute("True", button);
		}
	}
}