using System;
using System.Collections.ObjectModel;
using HotelApp.Models;
using HotelApp.Services;

namespace HotelApp.ViewModels
{
	public class MapViewModel
	{
		public ObservableCollection<RoomModel> Rooms { get; } = new ObservableCollection<RoomModel>();

		public MapViewModel(IntPtr hotelPtr)
		{
			Refresh(hotelPtr);
		}

		public void Refresh(IntPtr hotelPtr)
		{
			var buffer = new CRoom[1000];
			int count = NativeMethods.Hotel_GetAllRooms(hotelPtr, buffer, 1000);

			Rooms.Clear();
			for (int i = 0; i < count; i++)
			{
				Rooms.Add(new RoomModel
				{
					Id = buffer[i].Id,
					Number = buffer[i].Number,
					Capacity = buffer[i].Capacity,
					BasePrice = buffer[i].BasePrice,
					Status = buffer[i].Status,
					Type = buffer[i].Type,
					GuestName = buffer[i].GuestName,
					CheckInDate = buffer[i].CheckInDate,
					CheckOutDate = buffer[i].CheckOutDate
				});
			}
		}
	}
}