using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;
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
		private int _lastMenuIndex;
		// Инициализирует главное окно приложения.

		public MainWindow()
		{
			InitializeComponent();
			this.Loaded += MainWindow_Loaded;
			this.Closed += MainWindow_Closed;

			ToastService.ToastRequested += OnToastRequested;
			AnimationService.Changed += OnAnimationSettingsChanged;
		}
		// Освобождает подписки при закрытии главного окна.

		private void MainWindow_Closed(object sender, EventArgs e)
		{
			ToastService.ToastRequested -= OnToastRequested;
			AnimationService.Changed -= OnAnimationSettingsChanged;
		}
		// Выполняет инициализацию после загрузки главного окна.

		private void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			ApplyAnimationSettings();

			if (DataContext is MainViewModel vm)
			{
				HotkeyService.Register(this, vm);
			}
		}

		// Отключаем встроенные переходы MaterialDesign, если анимации выключены.
		private void OnAnimationSettingsChanged()
		{
			if (!Dispatcher.CheckAccess())
			{
				Dispatcher.BeginInvoke(new Action(ApplyAnimationSettings));
				return;
			}

			ApplyAnimationSettings();
		}
		// Применяет текущие настройки анимации интерфейса.

		private void ApplyAnimationSettings()
		{
			if (MainContent == null) return;
			try
			{
				TransitionAssist.SetDisableTransitions(MainContent, !AnimationService.Enabled);
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine("Не удалось применить настройку анимаций: " + ex.Message);
			}
		}

		// --- TOAST-УВЕДОМЛЕНИЯ ---

		private void OnToastRequested(ToastMessage toast)
		{
			if (toast == null) return;

			if (!Dispatcher.CheckAccess())
			{
				Dispatcher.BeginInvoke(new Action(() => OnToastRequested(toast)));
				return;
			}

			ShowToast(toast);
		}
		// Отображает всплывающее уведомление в интерфейсе.

		private void ShowToast(ToastMessage toast)
		{
			try
			{
				Brush background;
				PackIconKind iconKind;

				switch (toast.Type)
				{
					case ToastType.Error:
						background = new SolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28));
						iconKind = PackIconKind.AlertCircle;
						break;
					case ToastType.Info:
						background = new SolidColorBrush(Color.FromRgb(0x15, 0x65, 0xC0));
						iconKind = PackIconKind.Information;
						break;
					default:
						background = new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32));
						iconKind = PackIconKind.CheckCircle;
						break;
				}

				var icon = new PackIcon
				{
					Kind = iconKind,
					Width = 20,
					Height = 20,
					Foreground = Brushes.White,
					VerticalAlignment = VerticalAlignment.Center,
					Margin = new Thickness(0, 0, 10, 0)
				};

				var text = new TextBlock
				{
					Text = toast.Text,
					Foreground = Brushes.White,
					FontSize = 13,
					TextWrapping = TextWrapping.Wrap,
					MaxWidth = 360,
					VerticalAlignment = VerticalAlignment.Center
				};

				var content = new StackPanel { Orientation = Orientation.Horizontal };
				content.Children.Add(icon);
				content.Children.Add(text);

				var card = new Border
				{
					Background = background,
					CornerRadius = new CornerRadius(8),
					Padding = new Thickness(14, 10, 16, 10),
					Margin = new Thickness(0, 6, 0, 0),
					HorizontalAlignment = HorizontalAlignment.Right,
					Child = content,
					Effect = new DropShadowEffect
					{
						BlurRadius = 14,
						ShadowDepth = 2,
						Opacity = 0.35,
						Color = Colors.Black
					}
				};

				while (ToastHost.Children.Count >= 4)
					ToastHost.Children.RemoveAt(0);

				ToastHost.Children.Add(card);

				if (AnimationService.Enabled)
				{
					var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180));
					card.BeginAnimation(UIElement.OpacityProperty, fadeIn);
				}

				var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3.5) };
				timer.Tick += (s, e) =>
				{
					timer.Stop();

					if (AnimationService.Enabled)
					{
						var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(250));
						fadeOut.Completed += (s2, e2) => ToastHost.Children.Remove(card);
						card.BeginAnimation(UIElement.OpacityProperty, fadeOut);
					}
					else
					{
						ToastHost.Children.Remove(card);
					}
				};
				timer.Start();
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine("Не удалось показать уведомление: " + ex.Message);
			}
		}
		// Обновляет текущее представление главного окна.

		public void RefreshView()
		{
			if (DataContext is MainViewModel vm)
			{
				RefreshCurrentView(vm);
				vm.AddLog("Экран обновлен вручную.", LogType.Info);
			}
		}
		// Переключает текущее представление при выборе пункта меню.

		private void MenuList_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (DataContext is MainViewModel vm && MenuList != null)
			{
				if (MenuList.SelectedIndex == 4)
				{
					OpenSettingsDialog();
					return;
				}

				if (MenuList.SelectedIndex >= 0)
					_lastMenuIndex = MenuList.SelectedIndex;

				if (MenuList.SelectedIndex == 0) // Номера и Фонд
					vm.CurrentView = new RoomsViewModel(vm.HotelPtr, vm);

				else if (MenuList.SelectedIndex == 1) // База гостей
					vm.CurrentView = new GuestsView { DataContext = new GuestsViewModel(vm.HotelPtr, vm) };

				else if (MenuList.SelectedIndex == 2) // Аналитика
					vm.CurrentView = new AnalyticsView { DataContext = new AnalyticsViewModel(vm.HotelPtr) };

				else if (MenuList.SelectedIndex == 3) // Карта отеля
					vm.CurrentView = new MapView { DataContext = new MapViewModel(vm.HotelPtr) };
			}
		}

		// --- НАСТРОЙКИ ---
		private async void OpenSettingsDialog()
		{
			if (!(DataContext is MainViewModel vm)) return;

			if (DialogHost.IsDialogOpen("RootDialog"))
			{
				RestoreMenuSelection();
				return;
			}

			try
			{
				var settingsView = new SettingsView
				{
					DataContext = new SettingsViewModel(this, vm)
				};

				await DialogHost.Show(settingsView, "RootDialog");
			}
			catch (Exception ex)
			{
				vm.AddLog("Не удалось открыть настройки: " + ex.Message, LogType.Error);
				AppDialog.Error("Не удалось открыть настройки.\n" + ex.Message, "Настройки");
			}
			finally
			{
				RestoreMenuSelection();
				RestoreFocus();
			}
		}

		// без Activate() окно может остаться неактивным, и горячие клавиши не работают.
		private void RestoreFocus()
		{
			if (!IsLoaded) return;

			try
			{
				Activate();
				Keyboard.ClearFocus();

				if (MenuList != null && MenuList.IsVisible)
				{
					MenuList.Focus();
					Keyboard.Focus(MenuList);
				}
				else
				{
					Focus();
					Keyboard.Focus(this);
				}
			}
			catch (Exception ex)
			{
				if (DataContext is MainViewModel vm)
					vm.AddLog("Не удалось вернуть фокус главному окну: " + ex.Message, LogType.Error);
			}
		}

		// не пересоздавая текущее представление.
		private void RestoreMenuSelection()
		{
			if (MenuList == null) return;

			MenuList.SelectionChanged -= MenuList_SelectionChanged;
			MenuList.SelectedIndex = _lastMenuIndex;
			MenuList.SelectionChanged += MenuList_SelectionChanged;
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

			string templatesDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates");
			try { System.IO.Directory.CreateDirectory(templatesDir); } catch { }

			var dialog = new OpenFileDialog
			{
				Title = "Выберите файл Excel с гостями",
				Filter = "Книга Excel (*.xlsx)|*.xlsx|Все файлы (*.*)|*.*",
				InitialDirectory = System.IO.Directory.Exists(templatesDir) ? templatesDir : AppDomain.CurrentDomain.BaseDirectory
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

			ImportDialogHost.Content = content;
			ImportOverlay.Visibility = Visibility.Visible;
			ImportDialogHost.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
		}

		// --- ЗАКРЫТИЕ ОКНА ИМПОРТА ---
		private void CancelImport_Click(object sender, RoutedEventArgs e)
		{
			CloseImportOverlay();
		}
		// Закрывает окно импорта при щелчке по затемнённой области.

		private void ImportOverlay_Click(object sender, MouseButtonEventArgs e)
		{
			CloseImportOverlay();
		}
		// Обрабатывает нажатия клавиш в окне импорта.

		private void ImportOverlay_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Escape)
			{
				e.Handled = true;
				CloseImportOverlay();
			}
		}
		// Закрывает слой предпросмотра импорта.

		private void CloseImportOverlay()
		{
			ImportOverlay.Visibility = Visibility.Collapsed;
			ImportDialogHost.Content = null;
		}

		// --- О ПРОГРАММЕ ---
		private async void OpenAbout_Click(object sender, RoutedEventArgs e)
		{
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
		// Применяет подтверждённый импорт данных из Excel.

		private void ApplyImport(ImportPreviewViewModel preview, MainViewModel vm)
		{
			var summary = preview.ImportValid();

			vm.RefreshBuildings();
			if (vm.CurrentView is RoomsViewModel roomsVm)
			{
				roomsVm.RefreshData();
			}
			else if (vm.CurrentView is FrameworkElement view)
			{
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

		// --- СОХРАНЕНИЕ ТАБЛИЦЫ EXCEL (ТЕКУЩИЕ ДАННЫЕ ГОСТЕЙ) ---
		private void ExportTemplate_Click(object sender, RoutedEventArgs e)
		{
			if (!(DataContext is MainViewModel vm)) return;

			string templatesDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates");
			try { System.IO.Directory.CreateDirectory(templatesDir); } catch { }

			string defaultName = "Таблица_гостей_" + DateTime.Now.ToString("yyyy-MM-dd") + ".xlsx";
			var dialog = new SaveFileDialog
			{
				Title = "Сохранить таблицу Excel",
				Filter = "Книга Excel (*.xlsx)|*.xlsx",
				FileName = defaultName,
				InitialDirectory = System.IO.Directory.Exists(templatesDir) ? templatesDir : AppDomain.CurrentDomain.BaseDirectory
			};

			if (dialog.ShowDialog() != true) return;

			try
			{
				ExcelTemplate.SaveCurrentData(dialog.FileName, vm.HotelPtr);
				ToastService.Success("Таблица сохранена: " + System.IO.Path.GetFileName(dialog.FileName));
			}
			catch (Exception ex)
			{
				AppDialog.Error("Не удалось сохранить таблицу:\n" + ex.Message, "Сохранение таблицы");
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
				ToastService.Success("База данных очищена. Созданы корпуса «Главный корпус» и «Корпус А».");
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
				vm.UpdateStats();

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
		// Проверяет вставляемый текст в поле имени.

		private void Name_Pasting(object sender, DataObjectPastingEventArgs e)
		{
			string text = GetPastedText(e);
			if (text == null || !InputRules.IsAllowedNameInput(text)) e.CancelCommand();
		}
		// Ограничивает ввод в числовом поле только цифрами.

		private void Digits_PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			foreach (char c in e.Text)
			{
				if (char.IsDigit(c)) continue;
				e.Handled = true;
				return;
			}
		}
		// Проверяет вставляемый текст в числовое поле.

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
		// Ограничивает ввод в поле паспорта только цифрами.

		private void Passport_PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			foreach (char c in e.Text)
			{
				if (char.IsDigit(c)) continue;
				e.Handled = true;
				return;
			}
		}
		// Обрабатывает вставку текста в поле паспорта.

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
		// Ограничивает ввод символов в поле даты.

		private void Date_PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			var textBox = e.OriginalSource as TextBox;
			if (textBox == null || !InputRules.IsAllowedDateInput(textBox.Text, e.Text))
				e.Handled = true;
		}
		// Блокирует недопустимые клавиши при вводе даты.

		private void Date_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Space) e.Handled = true;
		}
		// Проверяет вставляемый текст в поле даты.

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
		// Извлекает текст из буфера обмена при вставке.

		private static string GetPastedText(DataObjectPastingEventArgs e)
		{
			if (!e.DataObject.GetDataPresent(typeof(string))) return null;
			return e.DataObject.GetData(typeof(string)) as string;
		}

		// Вызывают существующие private-обработчики, не ломая инкапсуляцию.
		public void TriggerRefresh() => RefreshCurrentView(DataContext as MainViewModel);
		// Запускает импорт данных из Excel.
		public void TriggerImportExcel() => ImportExcel_Click(null, null);
		// Открывает диалог добавления корпуса.
		public void TriggerAddBuilding() => OpenAddBuilding_Click(null, null);
		// Открывает диалог добавления номера.
		public void TriggerAddRoom() => OpenAddRoom_Click(null, null);
		// Открывает диалог сведений о программе.
		public void TriggerAbout() => OpenAbout_Click(null, null);
	}
}
