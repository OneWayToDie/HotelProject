#pragma once
#include "Room.h"
#include "Guest.h"
#include <vector>
#include <memory>
#include <string>

namespace HotelLogic {

    class Building {
    private:
        int id;
        std::string name;
        std::vector<std::shared_ptr<Room>> rooms; // Здание содержит номера (Композиция)

    public:
        Building(int id, const std::string& name);

        int GetId() const { return id; }
        std::string GetName() const { return name; }

        void AddRoom(std::shared_ptr<Room> room) { rooms.push_back(room); }
        const std::vector<std::shared_ptr<Room>>& GetRooms() const { return rooms; }
    };

    class Hotel {
    private:
        std::vector<std::shared_ptr<Building>> buildings; // Гостиница содержит здания
        std::vector<std::shared_ptr<Guest>> allGuests;
        int nextBuildingId;
        int nextRoomId;
        int nextGuestId = 1;
        int nextBookingId = 1;
        double totalRevenue = 0.0;

        // Создать корпус/номер с заранее заданным Id (для загрузки из файла)
        void AddBuildingWithId(int id, const std::string& name);
        void AddRoomWithId(int id, int buildingId, int roomType, const std::string& roomNumber, double price);

    public:
        Hotel();

        void AddBuilding(const std::string& name);
        void AddRoom(int buildingId, int roomType, const std::string& roomNumber, double price);

        // Бизнес-логика
        bool CheckIn(int roomId, const std::string& guestName, const std::string& passport, const std::string& inDate, const std::string& outDate, int days);
        double CheckOut(int roomId); // Возвращает итоговую сумму
        void FinishCleaning(int roomId);
        bool SaveToFile(const std::string& filename);
        bool LoadFromFile(const std::string& filename);
        void UpdateRoomPrice(int roomId, double newPrice);

        // Бронирование
        bool BookRoom(int roomId, const std::string& guestName, const std::string& passport, const std::string& inDate, const std::string& outDate);

        // Удалить гостя из базы по Id (false — гость не найден)
        bool DeleteGuest(int guestId);

        // Полная очистка базы. Создаются два корпуса по умолчанию, Id начинаются с 1.
        void ClearAll();

        // Поиск
        std::shared_ptr<Room> FindRoomById(int roomId);
        const std::vector<std::shared_ptr<Building>>& GetBuildings() const { return buildings; }
        std::vector<std::shared_ptr<Room>> GetAllRooms() const;
        const std::vector<std::shared_ptr<Guest>>& GetAllGuests() const { return allGuests; }

        // Все бронирования и общая выручка
        std::vector<std::shared_ptr<Booking>> GetAllBookings() const;
        double GetTotalRevenue() const { return totalRevenue; }
    };
}