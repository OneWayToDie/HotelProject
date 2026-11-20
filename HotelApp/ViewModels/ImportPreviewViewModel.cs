using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using HotelApp.Models;
using HotelApp.Services;

namespace HotelApp.ViewModels
{
	public class ImportPreviewViewModel : INotifyPropertyChanged
	{
		private readonly IntPtr _hotelPtr;

		// Получает коллекцию строк, подготовленных к предпросмотру импорта.
		public ObservableCollection<ImportRow> Rows { get; }

		// Создаёт модель представления предпросмотра импорта и выполняет анализ строк.
		public ImportPreviewViewModel(IntPtr hotelPtr, IEnumerable<ImportRow> rows)
		{
			_hotelPtr = hotelPtr;
			Rows = new ObservableCollection<ImportRow>(rows);
			HotelImporter.Analyze(Rows, hotelPtr);
		}

		// Возвращает общее количество строк импорта.
		public int TotalCount => Rows.Count;

		// Возвращает количество строк без ошибок.
		public int ValidCount => Rows.Count(r => !r.HasError);

		// Возвращает количество строк с ошибками.
		public int ErrorCount => Rows.Count(r => r.HasError);

		// Возвращает признак наличия строк, готовых к импорту.
		public bool HasValidRows => ValidCount > 0;

		// Возвращает текстовую сводку по результатам проверки импорта.
		public string SummaryText =>
			$"Всего строк: {TotalCount}   •   К импорту: {ValidCount}   •   С ошибками: {ErrorCount}";

		// Выполняет импорт допустимых строк и возвращает итоговую статистику.
		public ImportSummary ImportValid()
		{
			var summary = HotelImporter.Apply(Rows, _hotelPtr);
			NotifyStats();
			return summary;
		}

		// Уведомляет об изменении вычисляемых свойств статистики импорта.
		private void NotifyStats()
		{
			OnPropertyChanged(nameof(TotalCount));
			OnPropertyChanged(nameof(ValidCount));
			OnPropertyChanged(nameof(ErrorCount));
			OnPropertyChanged(nameof(HasValidRows));
			OnPropertyChanged(nameof(SummaryText));
		}

		// Событие уведомления об изменении значения свойства.
		public event PropertyChangedEventHandler PropertyChanged;

		// Уведомляет подписчиков об изменении свойства.
		protected void OnPropertyChanged([CallerMemberName] string name = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}
	}
}
