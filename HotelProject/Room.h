#pragma once
#include <string>
#include <iostream>

namespace HotelLogic {

    // Перечисление статусов номера
    enum class RoomStatus { Free = 0, Occupied = 1, Cleaning = 2 };

    // Базовый класс Номер
    class Room {
    protected:
        int id;
        std::string roomNumber;
        int capacity;       // Вместимость
        double basePrice;   // Цена за сутки
        RoomStatus status;
        std::string currentGuestName; // Кто живет
        std::string currentGuestPassport; // Паспорт
        std::string checkInDate;
        std::string checkOutDate;
        int bookedDays;
        // Эти три метода перенесены в public ниже

    public:
        // Конструктор
        Room(int id, std::string number, int cap, double price);

        // Виртуальный деструктор (обязательно для наследования)
        virtual ~Room() = default;

        // Геттеры (Инкапсуляция)
        int GetId() const { return id; }
        std::string GetNumber() const { return roomNumber; }
        int GetCapacity() const { return capacity; }
        double GetBasePrice() const { return basePrice; }
        RoomStatus GetStatus() const { return status; }

        // Геттеры и сеттеры для гостя (Перенесено в public!)
        std::string GetGuestName() const { return currentGuestName; }
        std::string GetGuestPassport() const { return currentGuestPassport; }
        void SetGuest(const std::string& name, const std::string& passport) {
            currentGuestName = name;
            currentGuestPassport = passport;
        }

        // Сеттеры
        void SetStatus(RoomStatus newStatus) { status = newStatus; }

        // Виртуальные методы (Полиморфизм)
        // В наследниках мы переопределим их, чтобы они возвращали правильные названия
        virtual std::string GetTypeName() const { return "Стандарт"; }
        virtual std::string GetAmenities() const { return "ТВ, Кровать"; }
        virtual double CalculatePrice(int days) const { return basePrice * days; }
        void SetPrice(double newPrice) { basePrice = newPrice; }
        void SetBookingDates(const std::string& inDate, const std::string& outDate, int days) {
            checkInDate = inDate;
            checkOutDate = outDate;
            bookedDays = days;
        }
        std::string GetCheckInDate() const { return checkInDate; }
        std::string GetCheckOutDate() const { return checkOutDate; }
        int GetBookedDays() const { return bookedDays; }
    };

    // Класс Люкс (Наследник)
    class LuxRoom : public Room {
    public:
        LuxRoom(int id, std::string number); // Цена и вместимость зададутся в .cpp
        std::string GetTypeName() const override { return "Люкс"; }
        std::string GetAmenities() const override { return "ТВ, King-size кровать, Джакузи, Мини-бар"; }
        double CalculatePrice(int days) const override; // У люкса может быть наценка
    };

    // Класс Конференц-зал (Наследник)
    class ConferenceRoom : public Room {
    public:
        ConferenceRoom(int id, std::string number);
        std::string GetTypeName() const override { return "Конференц-зал"; }
        std::string GetAmenities() const override { return "Проектор, Звук, 20 стульев"; }
        double CalculatePrice(int days) const override;
    };
}