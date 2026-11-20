using System;

namespace HotelApp.Services
{
	public enum ToastType
	{
		Success,
		Info,
		Error
	}

	public class ToastMessage
	{
		// Создаёт сообщение всплывающего уведомления.
		public ToastMessage(string text, ToastType type)
		{
			Text = text;
			Type = type;
		}

		// Получает текст уведомления.
		public string Text { get; }

		// Получает тип уведомления.
		public ToastType Type { get; }
	}

	public static class ToastService
	{
		// Событие запроса отображения всплывающего уведомления.
		public static event Action<ToastMessage> ToastRequested;

		// Показывает уведомление об успешном действии.
		public static void Success(string text)
		{
			Show(text, ToastType.Success);
		}

		// Показывает информационное уведомление.
		public static void Info(string text)
		{
			Show(text, ToastType.Info);
		}

		// Показывает уведомление об ошибке.
		public static void Error(string text)
		{
			Show(text, ToastType.Error);
		}

		// Отправляет запрос на отображение всплывающего уведомления.
		public static void Show(string text, ToastType type = ToastType.Success)
		{
			if (string.IsNullOrWhiteSpace(text)) return;
			ToastRequested?.Invoke(new ToastMessage(text.Trim(), type));
		}
	}
}
