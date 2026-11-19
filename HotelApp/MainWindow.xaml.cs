using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HotelApp.ViewModels;
using HotelApp.Views;
using HotelApp.Services;
using HotelApp.Models;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;

namespace HotelApp
{
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
			this.Loaded += MainWindow_Loaded;
		}

		private void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is MainViewModel vm)
			{
				// Регистрируем горячие клавиши
				HotkeyService.Register(this, vm);
			}
		}

		public void RefreshView()
		{
			if (DataContext is MainViewModel vm)
			{
				RefreshCurrentView(vm);
				vm.AddLog("Экран обновлен вручную.", LogType.Info);
			}
		}

		private void MenuList_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (DataContext is MainViewModel vm && MenuList != null)
			{
				if (MenuList.SelectedIndex == 0) // Номера и Фонд
					vm.CurrentView = new RoomsViewModel(vm.HotelPtr, vm);

				else if (MenuList.SelectedIndex == 1) // База гостей
					vm.CurrentView = new GuestsView { DataContext = new GuestsViewModel(vm.HotelPtr, vm) };

				else if (MenuList.SelectedIndex == 2) // Аналитика
					vm.CurrentView = new AnalyticsView { DataContext = new AnalyticsViewModel(vm.HotelPtr) };

				else if (MenuList.SelectedIndex == 3) // Карта отеля
					vm.CurrentView = new MapView { DataContext = new MapViewModel(vm.HotelPtr) };

				else if (MenuList.SelectedIndex == 4) // НАСТРОЙКИ
					OpenSettingsDialog();
			}
		}

		// --- НАСТРОЙКИ ---
		private async void OpenSettingsDialog()
		{
			if (!(DataContext is MainViewModel vm)) return;
			if (DialogHost.IsDialogOpen("RootDialog")) return;

			var settingsView = new SettingsView
			{
				DataContext = new SettingsViewModel(this, vm)
			};

			// Ждем закрытия диалога
			await DialogHost.Show(settingsView, "RootDialog");

			// === ЖЁСТКИЙ ВОССТАНОВЛЕНИЕ ФОКУСА ===
			// 1. Сбрасываем любой "залипший" фокус от DialogHost
			Keyboard.ClearFocus();

			// 2. Возвращаем фокус главному окну
			this.Focus();
			Keyboard.Focus(this);
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

				string buildingName = (textBox?.Text ?? string.Empty).Trim();
				if (!InputRules.IsValidBuildingName(buildingName))
				{
					AppDialog.Warning("Введите название корпуса (не более 50 символов)!", "Ошибка");
					return; // Окно не закрываем
				}

				NativeMethods.Hotel_AddBuilding(vm.HotelPtr, buildingName);
				vm.RefreshBuildings();
				if (vm.CurrentView is RoomsViewModel roomsVm)
				{
					roomsVm.RefreshData();
				}
				vm.AddLog($"Добавлен корпус: {buildingName}", LogType.Success);
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
				AppDialog.Warning("Выберите корпус!", "Ошибка");
				return; // Окно не закрываем
			}

			string roomNumber = (numberBox?.Text ?? string.Empty).Trim();
			if (!InputRules.IsValidRoomNumber(roomNumber))
			{
				AppDialog.Warning("Номер комнаты должен состоять только из цифр (до 5 знаков)!", "Ошибка");
				return; // Окно не закрываем
			}

			if (!InputRules.IsValidPrice(priceBox?.Text, out int price))
			{
				AppDialog.Warning("Введите корректную цену — целое число больше нуля!", "Ошибка");
				return; // Окно не закрываем
			}

			int buildingId = (int)buildingCombo.SelectedValue;
			int roomType = typeCombo?.SelectedIndex ?? 0;

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
			var passportBox = mainPanel.Children[2] as TextBox;
			var inPicker = mainPanel.Children[4] as DatePicker;
			var outPicker = mainPanel.Children[6] as DatePicker;

			string guestName = InputRules.NormalizeFullName(nameBox?.Text);
			if (!InputRules.IsValidFullName(guestName))
			{
				AppDialog.Warning("Введите ФИО гостя: только буквы, пробел и дефис.", "Ошибка");
				return; // Окно не закрываем
			}

			if (!InputRules.IsValidPassport(passportBox?.Text))
			{
				AppDialog.Warning("Паспорт должен быть в формате 1234 567890: 4 цифры, пробел, 6 цифр.", "Ошибка");
				return; // Окно не закрываем
			}

			if (!inPicker.SelectedDate.HasValue || !outPicker.SelectedDate.HasValue)
			{
				AppDialog.Warning("Выберите дату заезда и дату выезда (формат дд.мм.гггг)!", "Ошибка");
				return; // Окно не закрываем
			}

			if (outPicker.SelectedDate.Value.Date <= inPicker.SelectedDate.Value.Date)
			{
				AppDialog.Warning("Дата выезда должна быть позже даты заезда!", "Ошибка");
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
			var passportBox = mainPanel.Children[2] as TextBox;
			var inPicker = mainPanel.Children[4] as DatePicker;
			var outPicker = mainPanel.Children[6] as DatePicker;

			string guestName = InputRules.NormalizeFullName(nameBox?.Text);
			if (!InputRules.IsValidFullName(guestName))
			{
				AppDialog.Warning("Введите ФИО гостя: только буквы, пробел и дефис.", "Ошибка");
				return; // Окно не закрываем
			}

			if (!InputRules.IsValidPassport(passportBox?.Text))
			{
				AppDialog.Warning("Паспорт должен быть в формате 1234 567890: 4 цифры, пробел, 6 цифр.", "Ошибка");
				return; // Окно не закрываем
			}

			if (!inPicker.SelectedDate.HasValue || !outPicker.SelectedDate.HasValue)
			{
				AppDialog.Warning("Выберите дату заезда и дату выезда (формат дд.мм.гггг)!", "Ошибка");
				return; // Окно не закрываем
			}

			if (outPicker.SelectedDate.Value.Date <= inPicker.SelectedDate.Value.Date)
			{
				AppDialog.Warning("Дата выезда должна быть позже даты заезда!", "Ошибка");
				return; // Окно не закрываем
			}

			DialogHost.CloseDialogCommand.Execute("True", button);
		}

		// --- ИМПОРТ ИЗ EXCEL ---
		private void ImportExcel_Click(object sender, RoutedEventArgs e)
		{
			if (!(DataContext is MainViewModel vm)) return;

			var dialog = new OpenFileDialog
			{
				Title = "Выберите файл Excel с гостями",
				Filter = "Книга Excel (*.xlsx)|*.xlsx|Все файлы (*.*)|*.*"
			};

			if (dialog.ShowDialog() != true) return;

			List<ImportRow> rows;
			try
			{
				rows = ExcelImportService.Read(dialog.FileName);
			}
			catch (Exception ex)
			{
				vm.AddLog($"Импорт Excel: не удалось прочитать файл. {ex.Message}", LogType.Error);
				AppDialog.Error("Не удалось прочитать файл Excel:\n" + ex.Message, "Импорт Excel");
				return;
			}

			if (rows.Count == 0)
			{
				AppDialog.Warning("В файле нет строк с данными.", "Импорт Excel");
				return;
			}

			var preview = new ImportPreviewViewModel(vm.HotelPtr, rows);
			vm.AddLog($"Импорт Excel: прочитано строк — {preview.TotalCount}, корректных — {preview.ValidCount}.");

			var template = (DataTemplate)FindResource("ImportDialogTemplate");
			var content = template.LoadContent() as FrameworkElement;
			content.DataContext = preview;

			// Показываем во внутриоконном слое, а не в DialogHost/Popup:
			// прокрутка таблицы в слоёном Popup идёт ~6 fps, в окне — ~50 fps.
			ImportDialogHost.Content = content;
			ImportOverlay.Visibility = Visibility.Visible;
			ImportDialogHost.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
		}

		// --- ЗАКРЫТИЕ ОКНА ИМПОРТА ---
		private void CancelImport_Click(object sender, RoutedEventArgs e)
		{
			CloseImportOverlay();
		}

		private void ImportOverlay_Click(object sender, MouseButtonEventArgs e)
		{
			CloseImportOverlay();
		}

		private void ImportOverlay_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Escape)
			{
				e.Handled = true;
				CloseImportOverlay();
			}
		}

		private void CloseImportOverlay()
		{
			ImportOverlay.Visibility = Visibility.Collapsed;
			ImportDialogHost.Content = null;
		}

		// --- О ПРОГРАММЕ ---
		private async void OpenAbout_Click(object sender, RoutedEventArgs e)
		{
			// Убедимся, что DialogHost не открыт
			if (DialogHost.IsDialogOpen("RootDialog")) return;

			var template = (DataTemplate)FindResource("AboutDialogTemplate");
			var dialogContent = template.LoadContent() as FrameworkElement;
			await DialogHost.Show(dialogContent, "RootDialog");
		}

		// --- ССЫЛКА НА GITHUB ---
		private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
		{
			try
			{
				System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
				e.Handled = true;
			}
			catch { }
		}

		// --- ПОДТВЕРЖДЕНИЕ ИМПОРТА ---
		private void ConfirmImport_Click(object sender, RoutedEventArgs e)
		{
			if (!(sender is Button button)) return;
			if (!(button.DataContext is ImportPreviewViewModel preview)) return;
			if (!(DataContext is MainViewModel vm)) return;

			if (preview.ValidCount == 0)
			{
				AppDialog.Warning("Нет корректных строк для импорта.", "Импорт Excel");
				return;
			}

			try
			{
				ApplyImport(preview, vm);
			}
			catch (Exception ex)
			{
				vm.AddLog("Импорт Excel: ошибка применения. " + ex.Message, LogType.Error);
				AppDialog.Error("Не удалось применить импорт.\n" + ex.Message, "Импорт Excel");
			}
		}

		private void ApplyImport(ImportPreviewViewModel preview, MainViewModel vm)
		{
			var summary = preview.Apply();

			vm.RefreshBuildings();
			if (vm.CurrentView is RoomsViewModel roomsVm)
			{
				roomsVm.RefreshData();
			}
			else if (vm.CurrentView is FrameworkElement view)
			{
				// Вкладки «Гости», «Аналитика» и «Карта» хранят VM в DataContext представления
				if (view.DataContext is GuestsViewModel guestsVm) guestsVm.RefreshData();
				else if (view.DataContext is AnalyticsViewModel analyticsVm) analyticsVm.Refresh();
				else if (view.DataContext is MapViewModel mapVm) mapVm.Refresh(vm.HotelPtr);
			}
			vm.UpdateStats();
			vm.SaveData();

			vm.AddLog($"Импорт Excel: создано броней — {summary.ImportedRows}, номеров — {summary.CreatedRooms}, корпусов — {summary.CreatedBuildings}.",
				LogType.Success);
			if (summary.FailedRows > 0)
				vm.AddLog($"Импорт Excel: пропущено строк — {summary.FailedRows}.", LogType.Error);

			string message =
				"Импорт завершён.\n\n" +
				$"Броней создано: {summary.ImportedRows}\n" +
				$"Пропущено строк: {summary.FailedRows}\n" +
				$"Новых корпусов: {summary.CreatedBuildings}\n" +
				$"Новых номеров: {summary.CreatedRooms}";

			CloseImportOverlay();
			AppDialog.Info(message, "Импорт Excel");
		}

		// --- СОХРАНЕНИЕ ШАБЛОНА EXCEL ---
		private void ExportTemplate_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new SaveFileDialog
			{
				Title = "Сохранить шаблон Excel",
				Filter = "Книга Excel (*.xlsx)|*.xlsx",
				FileName = ExcelTemplate.DefaultFileName
			};

			if (dialog.ShowDialog() != true) return;

			try
			{
				ExcelTemplate.Save(dialog.FileName);
				AppDialog.Success("Шаблон сохранён:\n" + dialog.FileName, "Шаблон Excel");
			}
			catch (Exception ex)
			{
				AppDialog.Error("Не удалось сохранить шаблон:\n" + ex.Message, "Шаблон Excel");
			}
		}

		// --- ПОЛНАЯ ОЧИСТКА БАЗЫ ---
		private void ClearDatabase_Click(object sender, RoutedEventArgs e)
		{
			if (!(DataContext is MainViewModel vm)) return;

			bool confirmed = AppDialog.DestructiveConfirm(
				"Будут безвозвратно удалены:\n" +
				"• все корпуса и номера;\n" +
				"• все гости;\n" +
				"• все бронирования;\n" +
				"• накопленная выручка.\n\n" +
				"Будут созданы корпуса «Главный корпус» и «Корпус А». Нумерация ID начнётся с 1.\n" +
				"Отменить это действие нельзя.",
				"Очистка базы данных", "Очистить базу");

			if (!confirmed) return;

			try
			{
				NativeMethods.Hotel_ClearAll(vm.HotelPtr);
				vm.RefreshBuildings();
				vm.UpdateStats();
				RefreshCurrentView(vm);
				vm.SaveData();
				vm.AddLog("База данных очищена. Созданы корпуса «Главный корпус» и «Корпус А».",
					LogType.Success);
				AppDialog.Success("База данных очищена.\nСозданы корпуса «Главный корпус» и «Корпус А».",
					"Очистка базы данных");
			}
			catch (Exception ex)
			{
				vm.AddLog("Очистка базы данных не выполнена: " + ex.Message, LogType.Error);
				AppDialog.Error("Не удалось очистить базу данных:\n" + ex.Message,
					"Очистка базы данных");
			}
		}

		// Принудительное обновление текущего экрана и шапки приложения
		private void RefreshCurrentView(MainViewModel vm)
		{
			if (vm == null) return;

			try
			{
				// 1. Всегда обновляем статистику в шапке (Всего, Свободно, Занято)
				vm.UpdateStats();

				// 2. Обновляем ту вкладку, которая сейчас открыта
				if (vm.CurrentView is FrameworkElement view && view.DataContext != null)
				{
					if (view.DataContext is RoomsViewModel roomsVm)
					{
						roomsVm.RefreshData();
					}
					else if (view.DataContext is GuestsViewModel guestsVm)
					{
						guestsVm.RefreshData();
					}
					else if (view.DataContext is AnalyticsViewModel analyticsVm)
					{
						analyticsVm.Refresh();
					}
					else if (view.DataContext is MapViewModel mapVm)
					{
						mapVm.Refresh(vm.HotelPtr);
					}
				}
			}
			catch (Exception ex)
			{
				vm.AddLog($"Ошибка обновления экрана: {ex.Message}", LogType.Error);
			}
		}

		// --- ФИЛЬТРЫ ВВОДА ---

		private void Name_PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			if (!InputRules.IsAllowedNameInput(e.Text)) e.Handled = true;
		}

		private void Name_Pasting(object sender, DataObjectPastingEventArgs e)
		{
			string text = GetPastedText(e);
			if (text == null || !InputRules.IsAllowedNameInput(text)) e.CancelCommand();
		}

		private void Digits_PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			foreach (char c in e.Text)
			{
				if (char.IsDigit(c)) continue;
				e.Handled = true;
				return;
			}
		}

		private void Digits_Pasting(object sender, DataObjectPastingEventArgs e)
		{
			string text = GetPastedText(e);
			if (text == null) { e.CancelCommand(); return; }

			foreach (char c in text)
			{
				if (char.IsDigit(c)) continue;
				e.CancelCommand();
				return;
			}

			if (sender is TextBox tb && tb.MaxLength > 0 && tb.Text.Length + text.Length > tb.MaxLength)
				e.CancelCommand();
		}

		private void Passport_PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			foreach (char c in e.Text)
			{
				if (char.IsDigit(c)) continue;
				e.Handled = true;
				return;
			}
		}

		private void Passport_Pasting(object sender, DataObjectPastingEventArgs e)
		{
			string text = GetPastedText(e);
			if (text == null) { e.CancelCommand(); return; }

			foreach (char c in text)
			{
				if (char.IsDigit(c)) continue;
				e.CancelCommand();
				return;
			}

			if (sender is TextBox tb)
			{
				tb.Text = InputRules.FormatPassportDigits(text);
				tb.CaretIndex = tb.Text.Length;
			}
			e.CancelCommand();
		}

		// Автоматически ставим пробел после четвёртой цифры паспорта.
		private void Passport_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (!(sender is TextBox tb)) return;

			string formatted = InputRules.FormatPassportDigits(tb.Text);
			if (tb.Text == formatted) return;

			tb.Text = formatted;
			tb.CaretIndex = formatted.Length;
		}

		private void Date_PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			var textBox = e.OriginalSource as TextBox;
			if (textBox == null || !InputRules.IsAllowedDateInput(textBox.Text, e.Text))
				e.Handled = true;
		}

		private void Date_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Space) e.Handled = true;
		}

		private void Date_Pasting(object sender, DataObjectPastingEventArgs e)
		{
			string text = GetPastedText(e);
			if (text == null) { e.CancelCommand(); return; }

			foreach (char c in text)
			{
				if (char.IsDigit(c) || c == '.') continue;
				e.CancelCommand();
				return;
			}
		}

		private static string GetPastedText(DataObjectPastingEventArgs e)
		{
			if (!e.DataObject.GetDataPresent(typeof(string))) return null;
			return e.DataObject.GetData(typeof(string)) as string;
		}

		// --- ПУБЛИЧНЫЕ ОБЁРТКИ ДЛЯ ГОРЯЧИХ КЛАВИШ ---
		// Вызывают существующие private-обработчики, не ломая инкапсуляцию.
		public void TriggerRefresh() => RefreshCurrentView(DataContext as MainViewModel);
		public void TriggerImportExcel() => ImportExcel_Click(null, null);
		public void TriggerAddBuilding() => OpenAddBuilding_Click(null, null);
		public void TriggerAddRoom() => OpenAddRoom_Click(null, null);
		public void TriggerAbout() => OpenAbout_Click(null, null);
	}
}