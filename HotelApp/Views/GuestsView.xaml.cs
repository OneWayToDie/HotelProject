using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using HotelApp.Models;
using HotelApp.Services;
using HotelApp.ViewModels;

namespace HotelApp.Views
{
	/// <summary>
	/// Interaction logic for GuestsView.xaml
	/// </summary>
	public partial class GuestsView : UserControl
	{
		public GuestsView()
		{
			InitializeComponent();
		}

		// --- УДАЛЕНИЕ ГОСТЯ ИЗ БАЗЫ ---
		private void DeleteGuest_Click(object sender, RoutedEventArgs e)
		{
			if (!(DataContext is GuestsViewModel vm)) return;

			try
			{
				if (!(GuestsGrid.SelectedItem is GuestModel guest))
				{
					AppDialog.Warning("Выберите гостя в таблице.", "Удаление гостя");
					return;
				}

				bool confirmed = AppDialog.Confirm(
					$"Удалить гостя «{guest.FullName}» из базы?\nЗаписи о бронированиях сохранятся.",
					"Удаление гостя", "Удалить", "Отмена");

				if (!confirmed) return;

				vm.DeleteGuest(guest);
			}
			catch (Exception ex)
			{
				if (Application.Current.MainWindow?.DataContext is MainViewModel mainVm)
					mainVm.AddLog("Удаление гостя: " + ex.Message, LogType.Error);

				AppDialog.Error("Не удалось удалить гостя.\n" + ex.Message, "База гостей");
			}
		}
	}
}
