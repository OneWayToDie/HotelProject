using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using HotelApp.Models;
using HotelApp.Services;

namespace HotelApp.ViewModels
{
	public class MapViewModel
	{
		// Номера, сгруппированные по корпусам
		public ObservableCollection<BuildingMapModel> Buildings { get; } = new ObservableCollection<BuildingMapModel>();

		public MapViewModel(IntPtr hotelPtr)
		{
			Refresh(hotelPtr);
		}

		public void Refresh(IntPtr hotelPtr)
		{
			try
			{
				LoadMap(hotelPtr);
			}
			catch (Exception ex)
			{
				AppDialog.Error("Не удалось построить карту отеля.\n" + ex.Message, "Карта отеля");
			}
		}

		private void LoadMap(IntPtr hotelPtr)
		{
			// 1. Имена корпусов (BuildingId -> Название)
			var bBuffer = new CBuilding[100];
			int bCount = NativeMethods.Hotel_GetAllBuildings(hotelPtr, bBuffer, 100);

			var buildingNames = new Dictionary<int, string>();
			for (int i = 0; i < bCount; i++)
				buildingNames[bBuffer[i].Id] = bBuffer[i].Name;

			// 2. Все номера
			var buffer = new CRoom[1000];
			int count = NativeMethods.Hotel_GetAllRooms(hotelPtr, buffer, 1000);

			var rooms = new List<RoomModel>(count);
			for (int i = 0; i < count; i++)
			{
				rooms.Add(new RoomModel
				{
					Id = buffer[i].Id,
					BuildingId = buffer[i].BuildingId,
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

			// 3. Группируем по корпусу через LINQ
			Buildings.Clear();
			foreach (var group in rooms.GroupBy(r => r.BuildingId).OrderBy(g => g.Key))
			{
				Buildings.Add(new BuildingMapModel
				{
					BuildingId = group.Key,
					BuildingName = buildingNames.TryGetValue(group.Key, out string name)
						? name
						: $"Корпус {group.Key}",
					Rooms = new ObservableCollection<RoomModel>(group)
				});
			}
		}
	}
}
