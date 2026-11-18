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
	// Модель окна предпросмотра импорта
	public class ImportPreviewViewModel : INotifyPropertyChanged
	{
		private readonly IntPtr _hotelPtr;

		public ObservableCollection<ImportRow> Rows { get; }

		public ImportPreviewViewModel(IntPtr hotelPtr, IEnumerable<ImportRow> rows)
		{
			_hotelPtr = hotelPtr;
			Rows = new ObservableCollection<ImportRow>(rows);
			HotelImporter.Analyze(Rows, hotelPtr);
		}

		public int TotalCount => Rows.Count;
		public int ValidCount => Rows.Count(r => !r.HasError);
		public int ErrorCount => Rows.Count(r => r.HasError);
		public bool HasValidRows => ValidCount > 0;

		public string SummaryText =>
			$"Всего строк: {TotalCount}   •   К импорту: {ValidCount}   •   С ошибками: {ErrorCount}";

		public ImportSummary Apply()
		{
			var summary = HotelImporter.Apply(Rows, _hotelPtr);

			OnPropertyChanged(nameof(ValidCount));
			OnPropertyChanged(nameof(ErrorCount));
			OnPropertyChanged(nameof(HasValidRows));
			OnPropertyChanged(nameof(SummaryText));

			return summary;
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string name = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}
	}
}
