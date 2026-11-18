using System.Windows;
using HotelApp.Views.Dialogs;

namespace HotelApp.Services
{
	// Единая точка показа сообщений, предупреждений и подтверждений.
	// Окно стилизовано под приложение и работает из ViewModel без async.
	public static class AppDialog
	{
		public static void Info(string message, string title = "Информация")
		{
			Show(message, title, MessageDialogType.Info, MessageDialogButtons.Ok);
		}

		public static void Success(string message, string title = "Готово")
		{
			Show(message, title, MessageDialogType.Success, MessageDialogButtons.Ok);
		}

		public static void Warning(string message, string title = "Предупреждение")
		{
			Show(message, title, MessageDialogType.Warning, MessageDialogButtons.Ok);
		}

		public static void Error(string message, string title = "Ошибка")
		{
			Show(message, title, MessageDialogType.Error, MessageDialogButtons.Ok);
		}

		public static bool Confirm(string message, string title = "Подтверждение",
			string yesText = "Да", string noText = "Нет")
		{
			var dialog = new MessageDialog(title, message, MessageDialogType.Question,
				MessageDialogButtons.YesNo, yesText, noText);
			return ShowDialog(dialog) == MessageDialogResult.Yes;
		}

		public static bool DestructiveConfirm(string message, string title = "Подтверждение",
			string confirmText = "Удалить", string cancelText = "Отмена")
		{
			var dialog = new MessageDialog(title, message, MessageDialogType.Warning,
				MessageDialogButtons.OkCancel, confirmText, cancelText, destructive: true);
			return ShowDialog(dialog) == MessageDialogResult.Ok;
		}

		private static void Show(string message, string title, MessageDialogType type,
			MessageDialogButtons buttons)
		{
			var dialog = new MessageDialog(title, message, type, buttons);
			ShowDialog(dialog);
		}

		private static MessageDialogResult ShowDialog(MessageDialog dialog)
		{
			var owner = Application.Current?.MainWindow;
			if (owner != null && owner.IsLoaded && !ReferenceEquals(owner, dialog))
				dialog.Owner = owner;

			dialog.ShowDialog();
			return dialog.Result;
		}
	}
}
