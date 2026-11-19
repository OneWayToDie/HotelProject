using System.Windows;
using System.Windows.Controls;
using HotelApp.ViewModels;
using LiveCharts;
using LiveCharts.Wpf;
using MaterialDesignThemes.Wpf;

namespace HotelApp.Views
{
	public partial class AnalyticsView : UserControl
	{
		public AnalyticsView()
		{
			InitializeComponent();
		}

		private async void PieChart_DataClick(object sender, ChartPoint point)
		{
			// Проверяем, что клик был по сектору (PieSeries), а не по легенде
			if (point.SeriesView is PieSeries pieSeries && DataContext is AnalyticsViewModel vm)
			{
				string typeName = pieSeries.Title; // "Стандарт", "Люкс" или "Конференц-зал"

				// Вызываем команду ViewModel — она заполнит SelectedTypeGuests
				vm.ShowRoomTypeDetailsCommand.Execute(typeName);

				// Открываем модальное окно с результатами
				var template = (DataTemplate)Application.Current.MainWindow.FindResource("RoomTypeDetailsDialogTemplate");
				if (template == null) return;

				var dialogContent = template.LoadContent() as FrameworkElement;
				dialogContent.DataContext = vm; // Привязываем к тому же ViewModel, где уже есть данные

				await DialogHost.Show(dialogContent, "RootDialog");
			}
		}
	}
}
