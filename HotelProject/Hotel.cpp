#include "Hotel.h"
#include <iostream>
#include <fstream>
#include <sstream>

namespace HotelLogic 
{

    Building::Building(int id, const std::string& name) : id(id), name(name) {}

    Hotel::Hotel() : nextBuildingId(1), nextRoomId(1) {
        // Сразу создадим один корпус для теста
        AddBuilding("Главный корпус");
    }

    void Hotel::AddBuilding(const std::string& name) {
        buildings.push_back(std::make_shared<Building>(nextBuildingId++, name));
    }

    void Hotel::AddRoom(int buildingId, int roomType, const std::string& roomNumber, double price) {
        for (auto& b : buildings) {
            if (b->GetId() == buildingId) {
                std::shared_ptr<Room> newRoom;
                if (roomType == 1) newRoom = std::make_shared<LuxRoom>(nextRoomId++, roomNumber);
                else if (roomType == 2) newRoom = std::make_shared<ConferenceRoom>(nextRoomId++, roomNumber);
                else newRoom = std::make_shared<Room>(nextRoomId++, roomNumber, 2, price);

                // Устанавливаем переданную цену для любого типа
                newRoom->SetPrice(price);

                b->AddRoom(newRoom);
                return;
            }
        }
    }

    std::shared_ptr<Room> Hotel::FindRoomById(int roomId) {
        for (auto& b : buildings) {
            for (auto& r : b->GetRooms()) {
                if (r->GetId() == roomId) return r;
            }
        }
        return nullptr;
    }

    std::vector<std::shared_ptr<Room>> Hotel::GetAllRooms() const {
        std::vector<std::shared_ptr<Room>> all;
        for (auto& b : buildings) {
            for (auto& r : b->GetRooms()) {
                all.push_back(r);
            }
        }
        return all;
    }

    bool Hotel::CheckIn(int roomId, const std::string& guestName, const std::string& passport, const std::string& inDate, const std::string& outDate, int days) {
        auto room = FindRoomById(roomId);
        if (room && room->GetStatus() == RoomStatus::Free) {
            room->SetStatus(RoomStatus::Occupied);
            room->SetGuest(guestName, passport);
            room->SetBookingDates(inDate, outDate, days);

            // --- НОВОЕ: Добавляем гостя в базу, если его там еще нет ---
            bool exists = false;
            for (auto& g : allGuests) {
                if (g->passport == passport) { exists = true; break; }
            }
            if (!exists) {
                allGuests.push_back(std::make_shared<Guest>(nextGuestId++, guestName, passport));
            }
            // -----------------------------------------------------------

            return true;
        }
        return false;
    }

    double Hotel::CheckOut(int roomId) {
        auto room = FindRoomById(roomId);
        if (room && room->GetStatus() == RoomStatus::Occupied) {
            room->SetStatus(RoomStatus::Cleaning);

            // Берем дни, которые сохранили при заселении
            int daysStayed = room->GetBookedDays();
            if (daysStayed <= 0) daysStayed = 1; // Защита от дурака

            double total = room->CalculatePrice(daysStayed);
            totalRevenue += total;
            room->SetGuest("", "");
            return total;
        }
        return 0.0;
    }

    bool Hotel::BookRoom(int roomId, const std::string& guestName, const std::string& passport, const std::string& inDate, const std::string& outDate) {
        auto room = FindRoomById(roomId);
        if (!room) return false;
        if (inDate.empty() || outDate.empty() || inDate >= outDate) return false;

        // Проверяем пересечение с уже существующими бронями
        if (room->HasBookingConflict(inDate, outDate)) return false;

        auto booking = std::make_shared<Booking>(nextBookingId++, roomId, guestName, passport, inDate, outDate);
        room->AddBooking(booking);

        // Добавляем гостя в базу, если его там еще нет
        bool exists = false;
        for (auto& g : allGuests) {
            if (g->passport == passport) { exists = true; break; }
        }
        if (!exists) {
            allGuests.push_back(std::make_shared<Guest>(nextGuestId++, guestName, passport));
        }

        return true;
    }

    std::vector<std::shared_ptr<Booking>> Hotel::GetAllBookings() const {
        std::vector<std::shared_ptr<Booking>> all;
        for (auto& b : buildings) {
            for (auto& r : b->GetRooms()) {
                for (auto& booking : r->GetBookings()) {
                    all.push_back(booking);
                }
            }
        }
        return all;
    }

    void Hotel::UpdateRoomPrice(int roomId, double newPrice) {
        auto room = FindRoomById(roomId);
        if (room) room->SetPrice(newPrice);
    }

    void Hotel::FinishCleaning(int roomId) {
        auto room = FindRoomById(roomId);
        if (room && room->GetStatus() == RoomStatus::Cleaning) {
            room->SetStatus(RoomStatus::Free);
        }
    }

    bool Hotel::SaveToFile(const std::string& filename) {
        std::ofstream out(filename);
        if (!out.is_open()) return false;

        for (auto& b : buildings) {
            out << "BUILDING\t" << b->GetId() << "\t" << b->GetName() << "\n";

            for (auto& r : b->GetRooms()) {
                // Сохраняем ВСЕ поля: id, buildingId, номер, статус, тип, ЦЕНУ, гостя, паспорт, даты
                out << "ROOM\t" << r->GetId() << "\t" << b->GetId() << "\t"
                    << r->GetNumber() << "\t" << (int)r->GetStatus() << "\t"
                    << r->GetTypeName() << "\t" << r->GetBasePrice() << "\t"
                    << r->GetBookedDays() << "\t" 
                    << r->GetGuestName() << "\t" << r->GetGuestPassport() << "\t"
                    << r->GetCheckInDate() << "\t" << r->GetCheckOutDate() << "\n";
            }
        }
        // Сохраняем базу гостей
        for (auto& g : allGuests) {
            out << "GUEST\t" << g->id << "\t" << g->fullName << "\t" << g->passport << "\n";
        }

        // Сохраняем бронирования
        for (auto& b : buildings) {
            for (auto& r : b->GetRooms()) {
                for (auto& booking : r->GetBookings()) {
                    out << "BOOKING\t" << booking->GetId() << "\t" << booking->GetRoomId() << "\t"
                        << booking->GetGuestName() << "\t" << booking->GetGuestPassport() << "\t"
                        << booking->GetCheckInDate() << "\t" << booking->GetCheckOutDate() << "\n";
                }
            }
        }

        // Статистика (общая выручка)
        out << "STATS\t" << totalRevenue << "\n";
        out.close();
        return true;
    }

    bool Hotel::LoadFromFile(const std::string& filename) {
        std::ifstream in(filename);
        if (!in.is_open()) return false;

        // Очищаем текущие данные (ВСЁ, включая гостей!)
        buildings.clear();
        allGuests.clear();      // <--- ДОБАВИЛИ ОЧИСТКУ ГОСТЕЙ
        nextBuildingId = 1;
        nextRoomId = 1;
        nextGuestId = 1;        // <--- СБРАСЫВАЕМ СЧЕТЧИК ГОСТЕЙ
        nextBookingId = 1;
        totalRevenue = 0.0;

        std::string line;
        while (std::getline(in, line)) {
            std::stringstream ss(line);
            std::string type;
            std::getline(ss, type, '\t');

            if (type == "BUILDING") {
                std::string idStr, name;
                std::getline(ss, idStr, '\t');
                std::getline(ss, name, '\t');

                int id = 0;
                try { id = std::stoi(idStr); }
                catch (...) { continue; }

                AddBuilding(name);
                if (id >= nextBuildingId) nextBuildingId = id + 1;
            }
            else if (type == "ROOM") {
                std::string idStr, bIdStr, number, statusStr, typeName, priceStr, daysStr, guest, passport, inDate, outDate;
                std::getline(ss, idStr, '\t');
                std::getline(ss, bIdStr, '\t');
                std::getline(ss, number, '\t');
                std::getline(ss, statusStr, '\t');
                std::getline(ss, typeName, '\t');
                std::getline(ss, priceStr, '\t');
                std::getline(ss, daysStr, '\t');
                std::getline(ss, guest, '\t');
                std::getline(ss, passport, '\t');
                std::getline(ss, inDate, '\t');
                std::getline(ss, outDate, '\t');

                int id = 0, bId = 0, status = 0;
                try {
                    id = std::stoi(idStr);
                    bId = std::stoi(bIdStr);
                    status = std::stoi(statusStr);
                }
                catch (...) { continue; }

                double price = 2000.0;
                if (!priceStr.empty()) { try { price = std::stod(priceStr); } catch (...) {} }

                int days = 0;
                if (!daysStr.empty()) { try { days = std::stoi(daysStr); } catch (...) {} }

                int roomType = (typeName == "Люкс") ? 1 : (typeName == "Конференц-зал" ? 2 : 0);
                AddRoom(bId, roomType, number, price);

                auto room = FindRoomById(nextRoomId - 1);
                if (room) {
                    room->SetStatus((RoomStatus)status);
                    room->SetGuest(guest, passport);
                    room->SetBookingDates(inDate, outDate, days);
                }
                if (id >= nextRoomId) nextRoomId = id + 1;
            }
            // ==========================================
            // НОВЫЙ БЛОК ДЛЯ ЧТЕНИЯ ГОСТЕЙ
            // ==========================================
            else if (type == "GUEST") {
                std::string idStr, name, passport;
                std::getline(ss, idStr, '\t');
                std::getline(ss, name, '\t');
                std::getline(ss, passport, '\t');

                int id = 0;
                try { id = std::stoi(idStr); }
                catch (...) { continue; }

                allGuests.push_back(std::make_shared<Guest>(id, name, passport));
                if (id >= nextGuestId) nextGuestId = id + 1;
            }
            else if (type == "STATS") {
                std::string revenueStr;
                std::getline(ss, revenueStr, '\t');
                if (!revenueStr.empty()) {
                    try { totalRevenue = std::stod(revenueStr); } catch (...) {}
                }
            }
            else if (type == "BOOKING") {
                std::string idStr, roomIdStr, name, passport, inDate, outDate;
                std::getline(ss, idStr, '\t');
                std::getline(ss, roomIdStr, '\t');
                std::getline(ss, name, '\t');
                std::getline(ss, passport, '\t');
                std::getline(ss, inDate, '\t');
                std::getline(ss, outDate, '\t');

                int id = 0, roomId = 0;
                try {
                    id = std::stoi(idStr);
                    roomId = std::stoi(roomIdStr);
                }
                catch (...) { continue; }

                auto room = FindRoomById(roomId);
                if (room) {
                    room->AddBooking(std::make_shared<Booking>(id, roomId, name, passport, inDate, outDate));
                }
                if (id >= nextBookingId) nextBookingId = id + 1;
            }
        }
        in.close();
        return true;
    }
}