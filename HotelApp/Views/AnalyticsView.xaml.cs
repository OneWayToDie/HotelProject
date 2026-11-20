using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using HotelApp.Models;
using HotelApp.Services;
using HotelApp.ViewModels;
using LiveCharts;
using LiveCharts.Wpf;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;

namespace HotelApp.Views
{
	public partial class AnalyticsView : UserControl
	{
		public AnalyticsView()
		{
			InitializeComponent();
		}

		// В MaterialDesign 4.9 для WPF нет встроенного StaggeredEntrance,
		// поэтому делаем эквивалент: карточки по очереди всплывают и проявляются.
		private void AnalyticsView_Loaded(object sender, RoutedEventArgs e)
		{
			RunStaggeredEntrance();
		}

		private void RunStaggeredEntrance()
		{
			var cards = new List<FrameworkElement>();
			if (StatsGrid != null)
				cards.AddRange(StatsGrid.Children.OfType<FrameworkElement>());
			if (DetailCard != null)
				cards.Add(DetailCard);

			try
			{
				if (!AnimationService.Enabled)
				{
					foreach (var card in cards)
					{
						card.Opacity = 1;
						card.RenderTransform = Transform.Identity;
					}
					return;
				}

				for (int i = 0; i < cards.Count; i++)
				{
					var card = cards[i];
					var translate = new TranslateTransform(0, 18);

					card.Opacity = 0;
					card.RenderTransform = translate;

					var begin = TimeSpan.FromMilliseconds(i * 70);
					var duration = TimeSpan.FromMilliseconds(360);
					var easing = new CubicEase { EasingMode = EasingMode.EaseOut };

					var fade = new DoubleAnimation(0, 1, duration)
					{
						BeginTime = begin,
						EasingFunction = easing
					};
					var slide = new DoubleAnimation(18, 0, duration)
					{
						BeginTime = begin,
						EasingFunction = easing
					};

					card.BeginAnimation(UIElement.OpacityProperty, fade);
					translate.BeginAnimation(TranslateTransform.YProperty, slide);
				}
			}
			catch (Exception ex)
			{
				// Анимация не должна ломать экран: показываем карточки сразу.
				foreach (var card in cards)
					card.Opacity = 1;
				System.Diagnostics.Debug.WriteLine("Анимация карточек аналитики: " + ex.Message);
			}
		}

		// Экспорт аналитического отчёта: сводка по отелю + топ-5 гостей по сумме.
		private void ExportReport_Click(object sender, RoutedEventArgs e)
		{
			if (!(DataContext is AnalyticsViewModel vm)) return;

			var dialog = new SaveFileDialog
			{
				Title = "Сохранить аналитический отчёт",
				Filter = "Книга Excel (*.xlsx)|*.xlsx",
				DefaultExt = ".xlsx",
				FileName = "Аналитика_отеля_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm") + ".xlsx"
			};

			if (dialog.ShowDialog() != true) return;

			var mainVm = GetMainViewModel();
			try
			{
				// Обновляем показатели, чтобы отчёт не содержал устаревших цифр.
				vm.Refresh();
				AnalyticsReportService.Save(dialog.FileName, vm);

				mainVm?.AddLog("Аналитический отчёт сохранён: " + dialog.FileName, LogType.Success);
				ToastService.Success("Отчёт сохранён: " + System.IO.Path.GetFileName(dialog.FileName));
			}
			catch (Exception ex)
			{
				mainVm?.AddLog("Не удалось сохранить аналитический отчёт: " + ex.Message, LogType.Error);
				AppDialog.Error("Не удалось сохранить отчёт:\n" + ex.Message, "Экспорт отчёта");
			}
		}

		private static MainViewModel GetMainViewModel()
		{
			var window = Application.Current.MainWindow;
			return window != null ? window.DataContext as MainViewModel : null;
		}

		private bool _isDetailsDialogOpen;

		private async void PieChart_DataClick(object sender, ChartPoint point)
		{
			// Защита от двойного клика: второй диалог не должен вставать в очередь
			// за первым — именно это выглядело как «зависание» таблицы.
			if (_isDetailsDialogOpen) return;

			// Проверяем, что клик был по сектору (PieSeries), а не по легенде
			if (!(point.SeriesView is PieSeries pieSeries)) return;
			if (!(DataContext is AnalyticsViewModel vm)) return;

			_isDetailsDialogOpen = true;
			try
			{
				string typeName = pieSeries.Title; // "Стандарт", "Люкс" или "Конференц-зал"

				// Вызываем команду ViewModel — она заполнит SelectedTypeGuests
				vm.ShowRoomTypeDetailsCommand.Execute(typeName);

				// Открываем модальное окно с результатами
				var window = Application.Current.MainWindow;
				var template = window != null
					? window.FindResource("RoomTypeDetailsDialogTemplate") as DataTemplate
					: null;
				if (template == null) return;

				var dialogContent = template.LoadContent() as FrameworkElement;
				if (dialogContent == null) return;

				dialogContent.DataContext = vm; // Привязываем к тому же ViewModel, где уже есть данные

				await DialogHost.Show(dialogContent, "RootDialog");
			}
			catch (Exception ex)
			{
				AppDialog.Error("Не удалось открыть список гостей.\n" + ex.Message, "Аналитика");
			}
			finally
			{
				_isDetailsDialogOpen = false;
			}
		}
	}
}
