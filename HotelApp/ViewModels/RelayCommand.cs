using System;
using System.Windows.Input;

namespace HotelApp.ViewModels
{
	public class RelayCommand : ICommand
	{
		private readonly Action<object> _execute;
		private readonly Predicate<object> _canExecute;

		// Создаёт команду с указанными действиями выполнения и проверки доступности.
		public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
		{
			_execute = execute ?? throw new ArgumentNullException(nameof(execute));
			_canExecute = canExecute;
		}

		// Определяет, может ли команда выполняться в текущем состоянии.
		public bool CanExecute(object parameter)
		{
			return _canExecute == null || _canExecute(parameter);
		}

		// Выполняет команду с переданным параметром.
		public void Execute(object parameter)
		{
			_execute(parameter);
		}

		// Событие, уведомляющее об изменении возможности выполнения команды.
		public event EventHandler CanExecuteChanged
		{
			add => CommandManager.RequerySuggested += value;
			remove => CommandManager.RequerySuggested -= value;
		}
	}
}
