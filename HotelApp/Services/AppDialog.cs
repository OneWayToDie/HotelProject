using System.Windows;
using HotelApp.Views.Dialogs;

namespace HotelApp.Services
{
	public static class AppDialog
	{
		// Показывает информационное сообщение.
		public static void Info(string message, string title = "Информация")
		{
			Show(message, title, MessageDialogType.Info, MessageDialogButtons.Ok);
		}

		// Показывает сообщение об успешном выполнении действия.
		public static void Success(string message, string title = "Готово")
		{
			Show(message, title, MessageDialogType.Success, MessageDialogButtons.Ok);
		}

		// Показывает предупреждающее сообщение.
		public static void Warning(string message, string title = "Предупреждение")
		{
			Show(message, title, MessageDialogType.Warning, MessageDialogButtons.Ok);
		}

		// Показывает сообщение об ошибке.
		public static void Error(string message, string title = "Ошибка")
		{
			Show(message, title, MessageDialogType.Error, MessageDialogButtons.Ok);
		}

		// Показывает диалог подтверждения и возвращает результат выбора пользователя.
		public static bool Confirm(string message, string title = "Подтверждение",
			string yesText = "Да", string noText = "Нет")
		{
			var dialog = new MessageDialog(title, message, MessageDialogType.Question,
				MessageDialogButtons.YesNo, yesText, noText);
			return ShowDialog(dialog) == MessageDialogResult.Yes;
		}

		// Показывает диалог подтверждения опасного действия.
		public static bool DestructiveConfirm(string message, string title = "Подтверждение",
			string confirmText = "Удалить", string cancelText = "Отмена")
		{
			var dialog = new MessageDialog(title, message, MessageDialogType.Warning,
				MessageDialogButtons.OkCancel, confirmText, cancelText, destructive: true);
			return ShowDialog(dialog) == MessageDialogResult.Ok;
		}

		// Отображает стандартное диалоговое окно с заданным типом и набором кнопок.
		private static void Show(string message, string title, MessageDialogType type,
			MessageDialogButtons buttons)
		{
			var dialog = new MessageDialog(title, message, type, buttons);
			ShowDialog(dialog);
		}

		// Показывает диалоговое окно и возвращает результат его закрытия.
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
