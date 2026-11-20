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
		// Получает коллекцию корпусов с номерами для отображения на карте отеля.
		public ObservableCollection<BuildingMapModel> Buildings { get; } = new ObservableCollection<BuildingMapModel>();

		// Создаёт модель представления карты отеля и загружает данные.
		public MapViewModel(IntPtr hotelPtr)
		{
			Refresh(hotelPtr);
		}

		// Обновляет данные карты отеля.
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

		// Загружает данные корпусов и номеров для построения карты.
		private void LoadMap(IntPtr hotelPtr)
		{
			var bBuffer = new CBuilding[100];
			int bCount = NativeMethods.Hotel_GetAllBuildings(hotelPtr, bBuffer, 100);

			var buildingNames = new Dictionary<int, string>();
			for (int i = 0; i < bCount; i++)
				buildingNames[bBuffer[i].Id] = bBuffer[i].Name;

			var rBuffer = new CRoom[500];
			int rCount = NativeMethods.Hotel_GetAllRooms(hotelPtr, rBuffer, 500);

			var roomsByBuilding = new Dictionary<int, List<RoomModel>>();
			for (int i = 0; i < rCount; i++)
			{
				int bId = rBuffer[i].BuildingId;
				if (!roomsByBuilding.ContainsKey(bId))
					roomsByBuilding[bId] = new List<RoomModel>();

				roomsByBuilding[bId].Add(new RoomModel
				{
					Id = rBuffer[i].Id,
					BuildingId = rBuffer[i].BuildingId,
					Number = rBuffer[i].Number,
					Capacity = rBuffer[i].Capacity,
					BasePrice = rBuffer[i].BasePrice,
					Status = rBuffer[i].Status,
					Type = rBuffer[i].Type,
					GuestName = rBuffer[i].GuestName
				});
			}

			Buildings.Clear();
			foreach (var kvp in roomsByBuilding.OrderBy(b => b.Key))
			{
				string name = buildingNames.ContainsKey(kvp.Key)
					? buildingNames[kvp.Key]
					: $"Корпус {kvp.Key}";

				var buildingMap = new BuildingMapModel
				{
					BuildingId = kvp.Key,
					BuildingName = name
				};

				foreach (var room in kvp.Value.OrderBy(r => r.Number))
					buildingMap.Rooms.Add(room);

				Buildings.Add(buildingMap);
			}
		}
	}
}
