using System;
using System.Collections.ObjectModel;
using HotelApp.Models;
using HotelApp.Services;

namespace HotelApp.ViewModels
{
	public class GuestsViewModel
	{
		private IntPtr _hotelPtr;

		public ObservableCollection<GuestModel> Guests { get; } = new ObservableCollection<GuestModel>();

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

			Guests.Clear();
			for (int i = 0; i < count; i++)
			{
				Guests.Add(new GuestModel
				{
					Id = buffer[i].Id,
					FullName = buffer[i].FullName,
					Passport = buffer[i].Passport
				});
			}
		}
	}
}
