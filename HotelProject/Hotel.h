#pragma once
#include "Room.h"
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
        int nextBuildingId;
        int nextRoomId;

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

        // Поиск
        std::shared_ptr<Room> FindRoomById(int roomId);
        const std::vector<std::shared_ptr<Building>>& GetBuildings() const { return buildings; }
        std::vector<std::shared_ptr<Room>> GetAllRooms() const;
    };
}