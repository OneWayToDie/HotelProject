#pragma once                                                                 // Защита от повторного включения заголовочного файла
#include <string>                                                            // Подключение класса строк
#include <iostream>                                                          // Подключение потоков ввода-вывода
#include <vector>                                                            // Подключение контейнера вектор
#include <memory>                                                            // Подключение умных указателей
#include <algorithm>                                                         // Подключение алгоритмов стандартной библиотеки
#include "Booking.h"                                                         // Подключение объявления класса бронирования
                                                                             // Пустая строка для разделения секций
namespace HotelLogic {                                                       // Начало пространства имён логики отеля
                                                                             // Пустая строка перед перечислением статусов
    enum class RoomStatus { Free = 0, Occupied = 1, Cleaning = 2 };          // Перечисление возможных статусов номера
                                                                             // Пустая строка перед базовым классом номера
    class Room {                                                             // Базовый класс номера отеля
    protected:                                                               // Защищённые поля, доступные наследникам
        int id;                                                              // Уникальный идентификатор номера
        std::string roomNumber;                                              // Отображаемый номер комнаты
        int capacity;                                                        // Вместимость номера
        double basePrice;                                                    // Базовая цена за сутки
        RoomStatus status;                                                   // Текущий статус номера
        std::string currentGuestName;                                        // Имя текущего гостя
        std::string currentGuestPassport;                                    // Паспорт текущего гостя
        std::string checkInDate;                                             // Дата заезда текущего проживания
        std::string checkOutDate;                                            // Дата выезда текущего проживания
        int bookedDays;                                                      // Количество забронированных дней
        std::vector<std::shared_ptr<Booking>> bookings;                      // Список бронирований номера
                                                                             // Пустая строка перед публичной секцией
    public:                                                                  // Публичные члены класса
        Room(int id, std::string number, int cap, double price);             // Конструктор базового номера
                                                                             // Пустая строка перед виртуальным деструктором
        virtual ~Room() = default;                                           // Виртуальный деструктор по умолчанию
                                                                             // Пустая строка перед геттерами
        int GetId() const { return id; }                                     // Получение идентификатора номера
        std::string GetNumber() const { return roomNumber; }                 // Получение отображаемого номера
        int GetCapacity() const { return capacity; }                         // Получение вместимости номера
        double GetBasePrice() const { return basePrice; }                    // Получение базовой цены
        RoomStatus GetStatus() const { return status; }                      // Получение текущего статуса
                                                                             // Пустая строка перед методами работы с гостем
        std::string GetGuestName() const { return currentGuestName; }        // Получение имени текущего гостя
        std::string GetGuestPassport() const { return currentGuestPassport; }// Получение паспорта текущего гостя
        void SetGuest(const std::string& name, const std::string& passport) {// Установка данных текущего гостя
            currentGuestName = name;                                         // Сохранение имени гостя
            currentGuestPassport = passport;                                 // Сохранение паспорта гостя
        }                                                                    // Конец метода SetGuest
                                                                             // Пустая строка перед удалением брони по паспорту
        void RemoveBookingByPassport(const std::string& passport) {          // Удаление бронирований с указанным паспортом
            bookings.erase(                                                  // Удаление элементов из контейнера бронирований
                std::remove_if(bookings.begin(), bookings.end(),             // Поиск бронирований, удовлетворяющих условию
                    [&passport](const std::shared_ptr<Booking>& b) {         // Лямбда-функция сравнения паспорта
                        return b->GetGuestPassport() == passport;            // Проверка совпадения паспорта
                    }),                                                      // Конец лямбда-функции и remove_if
                bookings.end());                                             // Передача конечного итератора для erase
        }                                                                    // Конец метода RemoveBookingByPassport
                                                                             // Пустая строка перед изменением статуса
        void SetStatus(RoomStatus newStatus) { status = newStatus; }         // Установка нового статуса номера
                                                                             // Пустая строка перед виртуальными методами
        virtual std::string GetTypeName() const { return "Стандарт"; }       // Получение названия типа номера
        virtual std::string GetAmenities() const { return "ТВ, Душевая"; }  // Получение списка удобств номера
        virtual double CalculatePrice(int days) const { return basePrice * days; } // Расчёт стоимости проживания
        void SetPrice(double newPrice) { basePrice = newPrice; }             // Установка новой базовой цены
        void SetBookingDates(const std::string& inDate, const std::string& outDate, int days) { // Установка дат и длительности проживания
            checkInDate = inDate;                                            // Сохранение даты заезда
            checkOutDate = outDate;                                          // Сохранение даты выезда
            bookedDays = days;                                               // Сохранение количества дней
        }                                                                    // Конец метода SetBookingDates
        std::string GetCheckInDate() const { return checkInDate; }           // Получение даты заезда
        std::string GetCheckOutDate() const { return checkOutDate; }         // Получение даты выезда
        int GetBookedDays() const { return bookedDays; }                     // Получение количества забронированных дней
                                                                             // Пустая строка перед методами работы с бронями
        void AddBooking(std::shared_ptr<Booking> booking) { bookings.push_back(booking); } // Добавление новой брони
        const std::vector<std::shared_ptr<Booking>>& GetBookings() const { return bookings; } // Получение списка броней
                                                                             // Пустая строка перед проверкой конфликтов
        bool HasBookingConflict(const std::string& inDate, const std::string& outDate) const { // Проверка конфликтов по датам
            for (auto& booking : bookings) {                                 // Перебор всех существующих бронирований
                if (booking->Overlaps(inDate, outDate)) return true;         // Возврат true при обнаружении пересечения
            }                                                                // Конец цикла перебора бронирований
            return false;                                                    // Возврат false при отсутствии конфликтов
        }                                                                    // Конец метода HasBookingConflict
    };                                                                       // Конец класса Room
                                                                             // Пустая строка перед классом люкса
    class LuxRoom : public Room {                                            // Класс номера типа «Люкс»
    public:                                                                  // Публичные члены класса
        LuxRoom(int id, std::string number);                                 // Конструктор люкс-номера
        std::string GetTypeName() const override { return "Люкс"; }          // Получение названия типа номера
        std::string GetAmenities() const override { return "ТВ, King-size кровать, Джакузи, Мини-бар"; } // Получение списка удобств
        double CalculatePrice(int days) const override;                      // Расчёт стоимости проживания для люкса
    };                                                                       // Конец класса LuxRoom
                                                                             // Пустая строка перед классом конференц-зала
    class ConferenceRoom : public Room {                                     // Класс конференц-зала
    public:                                                                  // Публичные члены класса
        ConferenceRoom(int id, std::string number);                          // Конструктор конференц-зала
        std::string GetTypeName() const override { return "Конференц-зал"; } // Получение названия типа номера
        std::string GetAmenities() const override { return "Проектор, Экран, 20 кресел"; } // Получение списка удобств
        double CalculatePrice(int days) const override;                      // Расчёт стоимости аренды конференц-зала
    };                                                                       // Конец класса ConferenceRoom
}                                                                            // Конец пространства имён HotelLogic