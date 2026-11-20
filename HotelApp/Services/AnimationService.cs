using System;

namespace HotelApp.Services
{
	public static class AnimationService
	{
		// Получает признак включения анимаций интерфейса.
		public static bool Enabled { get; private set; } = true;

		// Событие изменения состояния анимаций.
		public static event Action Changed;

		// Инициализирует сервис анимаций начальным значением.
		public static void Initialize(bool enabled)
		{
			Enabled = enabled;
			Changed?.Invoke();
		}

		// Устанавливает текущее состояние анимаций интерфейса.
		public static void SetEnabled(bool enabled)
		{
			if (Enabled == enabled) return;
			Enabled = enabled;
			Changed?.Invoke();
		}
	}
}

