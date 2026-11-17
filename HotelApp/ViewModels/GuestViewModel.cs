using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using HotelApp.Models;
using HotelApp.Services;

namespace HotelApp.ViewModels
{
	public class GuestsViewModel : INotifyPropertyChanged
	{
		private IntPtr _hotelPtr;
		private string _searchText = string.Empty;

		public ObservableCollection<GuestModel> Guests { get; } = new ObservableCollection<GuestModel>();

		// Строка живого поиска (без кнопки "Найти")
		public string SearchText
		{
			get => _searchText;
			set
			{
				if (_searchText == value) return;
				_searchText = value;
				OnPropertyChanged();
				RefreshData();
			}
		}

		public GuestsViewModel(IntPtr hotelPtr)
		{
			_hotelPtr = hotelPtr;
			RefreshData();
		}

		public void RefreshData()
		{
			const int max = 1000;
			var buffer = new CGuest[max];
			int count = NativeMethods.Hotel_GetAllGuests(_hotelPtr, buffer, max);

			string search = (SearchText ?? string.Empty).Trim().ToLower();

			Guests.Clear();
			for (int i = 0; i < count; i++)
			{
				// ПРИМЕНЯЕМ ПОИСК (по ФИО и паспорту, без учета регистра)
				if (search.Length > 0)
				{
					string fullName = (buffer[i].FullName ?? string.Empty).ToLower();
					string passport = (buffer[i].Passport ?? string.Empty).ToLower();

					if (!fullName.Contains(search) && !passport.Contains(search)) continue;
				}

				Guests.Add(new GuestModel
				{
					Id = buffer[i].Id,
					FullName = buffer[i].FullName,
					Passport = buffer[i].Passport
				});
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string name = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}
	}
}
