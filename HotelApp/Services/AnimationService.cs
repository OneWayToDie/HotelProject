using System;

namespace HotelApp.Services
{
	// Глобальный флаг анимаций: читается представлениями при появлении
	// и меняется из настроек без перезапуска приложения.
	public static class AnimationService
	{
		public static bool Enabled { get; private set; } = true;

		public static event Action Changed;

		public static void Initialize(bool enabled)
		{
			Enabled = enabled;
			Changed?.Invoke();
		}

		public static void SetEnabled(bool enabled)
		{
			if (Enabled == enabled) return;
			Enabled = enabled;
			Changed?.Invoke();
		}
	}
}
