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
		private bool _isDetailsDialogOpen;

		// Инициализирует представление аналитики.
		public AnalyticsView()
		{
			InitializeComponent();
		}

		// Запускает анимацию появления элементов после загрузки представления.
		private void AnalyticsView_Loaded(object sender, RoutedEventArgs e)
		{
			RunStaggeredEntrance();
		}

		// Выполняет последовательную анимацию появления карточек аналитики.
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
				foreach (var card in cards)
					card.Opacity = 1;
				System.Diagnostics.Debug.WriteLine("Анимация карточек аналитики: " + ex.Message);
			}
		}

		// Экспортирует аналитический отчёт в файл Excel.
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

		// Возвращает главную модель представления текущего окна.
		private static MainViewModel GetMainViewModel()
		{
			var window = Application.Current.MainWindow;
			return window != null ? window.DataContext as MainViewModel : null;
		}

		// Открывает диалог с детализацией по выбранному типу номера.
		private async void PieChart_DataClick(object sender, ChartPoint point)
		{
			if (_isDetailsDialogOpen) return;

			if (!(point.SeriesView is PieSeries pieSeries)) return;
			if (!(DataContext is AnalyticsViewModel vm)) return;

			_isDetailsDialogOpen = true;
			try
			{
				string typeName = pieSeries.Title;

				vm.ShowRoomTypeDetailsCommand.Execute(typeName);

				var window = Application.Current.MainWindow;
				var template = window != null
					? window.FindResource("RoomTypeDetailsDialogTemplate") as DataTemplate
					: null;
				if (template == null) return;

				var dialogContent = template.LoadContent() as FrameworkElement;
				if (dialogContent == null) return;

				dialogContent.DataContext = vm;

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