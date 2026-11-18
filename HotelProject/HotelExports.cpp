#include "Hotel.h"
#include <cstring>
#include <vector>

using namespace HotelLogic;

#pragma pack(push, 1)
struct CRoom {
    int id;
    int buildingId;
    char number[20];
    int capacity;
    double basePrice;
    int status;         // 0-Free, 1-Occupied, 2-Cleaning
    int type;           // 0-Standard, 1-Lux, 2-Conference
    char guestName[100];
    char guestPassport[50];
    char checkInDate[20];
    char checkOutDate[20];
};

struct CBuilding {
    int id;
    char name[100];
};

struct CGuest {
    int id;
    char fullName[100];
    char passport[50];
};

struct CBooking {
    int id;
    int roomId;
    char guestName[100];
    char passport[50];
    char checkInDate[20];
    char checkOutDate[20];
};
#pragma pack(pop)

namespace {
    void CopyStr(char* dest, size_t size, const std::string& src) {
        strncpy_s(dest, size, src.c_str(), size - 1);
    }
}

extern "C" {
    __declspec(dllexport) void* Hotel_Create() {
        return new Hotel();
    }

    __declspec(dllexport) void Hotel_Destroy(void* ptr) {
        if (ptr) delete static_cast<Hotel*>(ptr);
    }

    __declspec(dllexport) void Hotel_AddBuilding(void* ptr, const char* name) {
        if (ptr) static_cast<Hotel*>(ptr)->AddBuilding(name);
    }

    __declspec(dllexport) void Hotel_AddRoom(void* ptr, int buildingId, int type, const char* number, double price) {
        if (ptr) static_cast<Hotel*>(ptr)->AddRoom(buildingId, type, number, price);
    }

    __declspec(dllexport) bool Hotel_CheckIn(void* ptr, int roomId, const char* name, const char* passport, const char* inDate, const char* outDate, int days) {
        if (!ptr) return false;
        return static_cast<Hotel*>(ptr)->CheckIn(roomId, name, passport, inDate, outDate, days);
    }

    __declspec(dllexport) double Hotel_CheckOut(void* ptr, int roomId) {
        if (!ptr) return 0.0;
        return static_cast<Hotel*>(ptr)->CheckOut(roomId);
    }

    __declspec(dllexport) void Hotel_FinishCleaning(void* ptr, int roomId) {
        if (ptr) static_cast<Hotel*>(ptr)->FinishCleaning(roomId);
    }

    __declspec(dllexport) void Hotel_UpdatePrice(void* ptr, int roomId, double newPrice) {
        if (ptr) static_cast<Hotel*>(ptr)->UpdateRoomPrice(roomId, newPrice);
    }

    __declspec(dllexport) int Hotel_GetAllRooms(void* ptr, CRoom* buffer, int maxSize) {
        if (!ptr) return 0;
        Hotel* hotel = static_cast<Hotel*>(ptr);

        // Обходим корпуса и их номера, чтобы заполнить buildingId
        int index = 0;
        for (auto& b : hotel->GetBuildings()) {
            for (auto& r : b->GetRooms()) {
                if (index >= maxSize) break;

                buffer[index].id = r->GetId();
                buffer[index].buildingId = b->GetId();
                CopyStr(buffer[index].number, sizeof(buffer[index].number), r->GetNumber());
                buffer[index].capacity = r->GetCapacity();
                buffer[index].basePrice = r->GetBasePrice();
                buffer[index].status = (int)r->GetStatus();

                std::string typeName = r->GetTypeName();
                if (typeName == "Люкс") buffer[index].type = 1;
                else if (typeName == "Конференц-зал") buffer[index].type = 2;
                else buffer[index].type = 0;

                CopyStr(buffer[index].guestName, sizeof(buffer[index].guestName), r->GetGuestName());
                CopyStr(buffer[index].guestPassport, sizeof(buffer[index].guestPassport), r->GetGuestPassport());

                CopyStr(buffer[index].checkInDate, sizeof(buffer[index].checkInDate), r->GetCheckInDate());
                CopyStr(buffer[index].checkOutDate, sizeof(buffer[index].checkOutDate), r->GetCheckOutDate());

                index++;
            }
        }
        return index;
    }

    __declspec(dllexport) int Hotel_GetAllBuildings(void* ptr, CBuilding* buffer, int maxSize) {
        if (!ptr) return 0;
        Hotel* hotel = static_cast<Hotel*>(ptr);
        auto bldgs = hotel->GetBuildings();

        int count = (int)bldgs.size();
        if (count > maxSize) count = maxSize;

        for (int i = 0; i < count; i++) {
            buffer[i].id = bldgs[i]->GetId();
            CopyStr(buffer[i].name, sizeof(buffer[i].name), bldgs[i]->GetName());
        }
        return count;
    }

    __declspec(dllexport) bool Hotel_Save(void* ptr, const char* filename) {
        if (!ptr) return false;
        return static_cast<Hotel*>(ptr)->SaveToFile(filename);
    }

    __declspec(dllexport) bool Hotel_Load(void* ptr, const char* filename) {
        if (!ptr) return false;
        return static_cast<Hotel*>(ptr)->LoadFromFile(filename);
    }

    // ФУНКЦИЯ ДЛЯ ПОЛУЧЕНИЯ ГОСТЕЙ (ТОЛЬКО ОДИН РАЗ)
    __declspec(dllexport) int Hotel_GetAllGuests(void* ptr, CGuest* buffer, int maxSize) {
        if (!ptr) return 0;
        Hotel* hotel = static_cast<Hotel*>(ptr);
        auto guests = hotel->GetAllGuests();
        int count = (int)guests.size();
        if (count > maxSize) count = maxSize;

        for (int i = 0; i < count; i++) {
            buffer[i].id = guests[i]->id;
            CopyStr(buffer[i].fullName, sizeof(buffer[i].fullName), guests[i]->fullName);
            CopyStr(buffer[i].passport, sizeof(buffer[i].passport), guests[i]->passport);
        }
        return count;
    }

    // Удалить гостя из базы (Id остальных гостей не меняются)
    // Полная очистка базы (Id начинаются с 1, создаются два корпуса по умолчанию)
    __declspec(dllexport) void Hotel_ClearAll(void* ptr) {
        if (!ptr) return;
        static_cast<Hotel*>(ptr)->ClearAll();
    }

    __declspec(dllexport) bool Hotel_DeleteGuest(void* ptr, int guestId) {
        if (!ptr) return false;
        return static_cast<Hotel*>(ptr)->DeleteGuest(guestId);
    }

    __declspec(dllexport) double Hotel_GetTotalRevenue(void* ptr) {
        if (!ptr) return 0.0;
        return static_cast<Hotel*>(ptr)->GetTotalRevenue();
    }

    __declspec(dllexport) bool Hotel_BookRoom(void* ptr, int roomId, const char* name, const char* passport, const char* inDate, const char* outDate) {
        if (!ptr) return false;
        return static_cast<Hotel*>(ptr)->BookRoom(roomId, name, passport, inDate, outDate);
    }

    __declspec(dllexport) int Hotel_GetAllBookings(void* ptr, CBooking* buffer, int maxSize) {
        if (!ptr) return 0;
        Hotel* hotel = static_cast<Hotel*>(ptr);
        auto bookings = hotel->GetAllBookings();
        int count = (int)bookings.size();
        if (count > maxSize) count = maxSize;

        for (int i = 0; i < count; i++) {
            auto b = bookings[i];
            buffer[i].id = b->GetId();
            buffer[i].roomId = b->GetRoomId();
            CopyStr(buffer[i].guestName, sizeof(buffer[i].guestName), b->GetGuestName());
            CopyStr(buffer[i].passport, sizeof(buffer[i].passport), b->GetGuestPassport());
            CopyStr(buffer[i].checkInDate, sizeof(buffer[i].checkInDate), b->GetCheckInDate());
            CopyStr(buffer[i].checkOutDate, sizeof(buffer[i].checkOutDate), b->GetCheckOutDate());
        }
        return count;
    }
}