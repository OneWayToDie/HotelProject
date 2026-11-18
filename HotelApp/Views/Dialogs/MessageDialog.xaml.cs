using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;

namespace HotelApp.Views.Dialogs
{
	public enum MessageDialogType
	{
		Info,
		Success,
		Warning,
		Error,
		Question
	}

	public enum MessageDialogButtons
	{
		Ok,
		YesNo,
		OkCancel
	}

	public enum MessageDialogResult
	{
		Ok,
		Yes,
		No,
		Cancel
	}

	// Единое окно сообщений в стиле приложения.
	public partial class MessageDialog : Window
	{
		private readonly MessageDialogButtons _buttons;
		private readonly bool _destructive;

		public MessageDialogResult Result { get; private set; } = MessageDialogResult.Cancel;

		public MessageDialog(string title, string message, MessageDialogType type,
			MessageDialogButtons buttons, string primaryText = null, string secondaryText = null,
			bool destructive = false)
		{
			InitializeComponent();

			_buttons = buttons;
			_destructive = destructive;

			Title = title ?? "Сообщение";
			TitleText.Text = Title;
			MessageText.Text = message ?? string.Empty;

			ConfigureIcon(type);
			ConfigureButtons(primaryText, secondaryText);
		}

		private void ConfigureIcon(MessageDialogType type)
		{
			switch (type)
			{
				case MessageDialogType.Success:
					DialogIcon.Kind = PackIconKind.CheckCircleOutline;
					DialogIcon.Foreground = Brushes.MediumSeaGreen;
					break;
				case MessageDialogType.Warning:
					DialogIcon.Kind = PackIconKind.AlertOutline;
					DialogIcon.Foreground = Brushes.Orange;
					break;
				case MessageDialogType.Error:
					DialogIcon.Kind = PackIconKind.CloseCircleOutline;
					DialogIcon.Foreground = Brushes.IndianRed;
					break;
				case MessageDialogType.Question:
					DialogIcon.Kind = PackIconKind.HelpCircleOutline;
					DialogIcon.Foreground = (Brush)FindResource("PrimaryHueMidBrush");
					break;
				default:
					DialogIcon.Kind = PackIconKind.InformationOutline;
					DialogIcon.Foreground = (Brush)FindResource("PrimaryHueMidBrush");
					break;
			}
		}

		private void ConfigureButtons(string primaryText, string secondaryText)
		{
			switch (_buttons)
			{
				case MessageDialogButtons.YesNo:
					PrimaryButton.Content = string.IsNullOrWhiteSpace(primaryText) ? "Да" : primaryText;
					SecondaryButton.Content = string.IsNullOrWhiteSpace(secondaryText) ? "Нет" : secondaryText;
					break;
				case MessageDialogButtons.OkCancel:
					PrimaryButton.Content = string.IsNullOrWhiteSpace(primaryText) ? "ОК" : primaryText;
					SecondaryButton.Content = string.IsNullOrWhiteSpace(secondaryText) ? "Отмена" : secondaryText;
					break;
				default:
					PrimaryButton.Content = string.IsNullOrWhiteSpace(primaryText) ? "ОК" : primaryText;
					SecondaryButton.Visibility = Visibility.Collapsed;
					break;
			}

			if (!_destructive) return;

			// Опасное действие: красная кнопка и обязательное подтверждение
			PrimaryButton.Content = string.IsNullOrWhiteSpace(primaryText) ? "Удалить" : primaryText;
			SecondaryButton.Content = string.IsNullOrWhiteSpace(secondaryText) ? "Отмена" : secondaryText;
			PrimaryButton.Background = new SolidColorBrush(Color.FromRgb(198, 40, 40));
			PrimaryButton.Foreground = Brushes.White;
			PrimaryButton.IsEnabled = false;
			ConfirmCheckBox.Visibility = Visibility.Visible;
		}

		private void ConfirmCheckBox_Changed(object sender, RoutedEventArgs e)
		{
			PrimaryButton.IsEnabled = ConfirmCheckBox.IsChecked == true;
		}

		private void Primary_Click(object sender, RoutedEventArgs e)
		{
			if (_destructive && ConfirmCheckBox.IsChecked != true) return;

			switch (_buttons)
			{
				case MessageDialogButtons.YesNo:
					Result = MessageDialogResult.Yes;
					break;
				default:
					Result = MessageDialogResult.Ok;
					break;
			}

			DialogResult = true;
		}

		private void Secondary_Click(object sender, RoutedEventArgs e)
		{
			Result = _buttons == MessageDialogButtons.YesNo
				? MessageDialogResult.No
				: MessageDialogResult.Cancel;
			DialogResult = false;
		}

		private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Escape) return;

			e.Handled = true;
			Result = _buttons == MessageDialogButtons.YesNo
				? MessageDialogResult.No
				: MessageDialogResult.Cancel;
			Close();
		}
	}
}
