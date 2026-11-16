#include "Room.h"

namespace HotelLogic {

    // Реализация базового класса
    Room::Room(int id, std::string number, int cap, double price)
        : id(id), roomNumber(number), capacity(cap), basePrice(price), status(RoomStatus::Free) {
    }

    // Реализация Люкса
    // По умолчанию Люкс: ID, Номер, Вместимость 2, Цена 5000
    LuxRoom::LuxRoom(int id, std::string number)
        : Room(id, number, 2, 5000.0) {
    }

    double LuxRoom::CalculatePrice(int days) const {
        // У люкса наценка за обслуживание 10%
        return (basePrice * days) * 1.1;
    }

    // Реализация Конференц-зала
    // По умолчанию: ID, Номер, Вместимость 20, Цена 10000
    ConferenceRoom::ConferenceRoom(int id, std::string number)
        : Room(id, number, 20, 10000.0) {
    }

    double ConferenceRoom::CalculatePrice(int days) const {
        // Конференц-зал сдается посуточно
        return basePrice * days;
    }
}