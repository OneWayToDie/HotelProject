#pragma once
#include <string>

namespace HotelLogic {

    // Класс Booking — бронирование номера на конкретные даты
    class Booking {
    private:
        int id;
        int roomId;
        std::string guestName;
        std::string passport;
        std::string checkInDate;
        std::string checkOutDate;

    public:
        Booking(int id, int roomId, const std::string& name, const std::string& guestPassport,
                const std::string& inDate, const std::string& outDate)
            : id(id), roomId(roomId), guestName(name), passport(guestPassport),
              checkInDate(inDate), checkOutDate(outDate) {
        }

        int GetId() const { return id; }
        int GetRoomId() const { return roomId; }
        std::string GetGuestName() const { return guestName; }
        std::string GetGuestPassport() const { return passport; }
        std::string GetCheckInDate() const { return checkInDate; }
        std::string GetCheckOutDate() const { return checkOutDate; }

        // Проверка пересечения дат (даты в формате yyyy-MM-dd сравниваются лексикографически)
        bool Overlaps(const std::string& inDate, const std::string& outDate) const {
            return inDate < checkOutDate && checkInDate < outDate;
        }
    };
}
