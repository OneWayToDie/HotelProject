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
		public ToastMessage(string text, ToastType type)
		{
			Text = text;
			Type = type;
		}

		public string Text { get; }
		public ToastType Type { get; }
	}

	// Ненавязчивые всплывающие уведомления в правом нижнем углу главного окна.
	// Главное окно подписывается на событие и рисует карточку само.
	public static class ToastService
	{
		public static event Action<ToastMessage> ToastRequested;

		public static void Success(string text) => Show(text, ToastType.Success);
		public static void Info(string text) => Show(text, ToastType.Info);
		public static void Error(string text) => Show(text, ToastType.Error);

		public static void Show(string text, ToastType type = ToastType.Success)
		{
			if (string.IsNullOrWhiteSpace(text)) return;
			ToastRequested?.Invoke(new ToastMessage(text.Trim(), type));
		}
	}
}
