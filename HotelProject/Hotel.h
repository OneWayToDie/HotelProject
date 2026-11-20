#pragma once                                                                 // Защита от повторного включения заголовочного файла
#include "Room.h"                                                            // Подключение объявления классов номеров
#include "Guest.h"                                                           // Подключение объявления класса гостя
#include <vector>                                                            // Подключение контейнера вектор
#include <memory>                                                            // Подключение умных указателей
#include <string>                                                            // Подключение класса строк
                                                                             // Пустая строка для разделения секций
namespace HotelLogic {                                                       // Начало пространства имён логики отеля
                                                                             // Пустая строка перед классом корпуса
    class Building {                                                         // Класс корпуса отеля
    private:                                                                 // Приватные поля корпуса
        int id;                                                              // Уникальный идентификатор корпуса
        std::string name;                                                    // Название корпуса
        std::vector<std::shared_ptr<Room>> rooms;                           // Список номеров корпуса
                                                                             // Пустая строка перед публичной секцией
    public:                                                                  // Публичные члены класса
        Building(int id, const std::string& name);                           // Конструктор корпуса
                                                                             // Пустая строка перед геттерами
        int GetId() const { return id; }                                     // Получение идентификатора корпуса
        std::string GetName() const { return name; }                         // Получение названия корпуса
                                                                             // Пустая строка перед методами работы с номерами
        void AddRoom(std::shared_ptr<Room> room) { rooms.push_back(room); }  // Добавление номера в корпус
        const std::vector<std::shared_ptr<Room>>& GetRooms() const { return rooms; } // Получение списка номеров корпуса
    };                                                                       // Конец класса Building
                                                                             // Пустая строка перед классом отеля
    class Hotel {                                                            // Класс отеля, управляющий данными и операциями
    private:                                                                 // Приватные поля и вспомогательные методы
        std::vector<std::shared_ptr<Building>> buildings;                   // Список корпусов отеля
        std::vector<std::shared_ptr<Guest>> allGuests;                       // Список всех гостей
        int nextBuildingId;                                                  // Следующий идентификатор корпуса
        int nextRoomId;                                                      // Следующий идентификатор номера
        int nextGuestId = 1;                                                 // Следующий идентификатор гостя
        int nextBookingId = 1;                                               // Следующий идентификатор бронирования
        double totalRevenue = 0.0;                                           // Накопленная выручка отеля
                                                                             // Пустая строка перед приватными методами
        void AddBuildingWithId(int id, const std::string& name);             // Добавление корпуса с заданным идентификатором
        void AddRoomWithId(int id, int buildingId, int roomType,             // Добавление номера с заданным идентификатором
                           const std::string& roomNumber, double price);     // Параметры номера при восстановлении из файла
                                                                             // Пустая строка перед публичной секцией
    public:                                                                  // Публичный интерфейс класса
        Hotel();                                                             // Конструктор отеля
                                                                             // Пустая строка перед операциями с корпусами и номерами
        void AddBuilding(const std::string& name);                           // Добавление нового корпуса
        void AddRoom(int buildingId, int roomType,                           // Добавление нового номера в корпус
                     const std::string& roomNumber, double price);           // Параметры создаваемого номера
                                                                             // Пустая строка перед операциями заселения и выселения
        bool CheckIn(int roomId, const std::string& guestName,               // Заселение гостя в номер
                     const std::string& passport,                            // Паспортные данные гостя
                     const std::string& inDate,                              // Дата заезда
                     const std::string& outDate, int days);                  // Дата выезда и количество дней
        double CheckOut(int roomId);                                         // Выселение гостя и расчёт суммы
        void FinishCleaning(int roomId);                                     // Завершение уборки номера
        void UpdateRoomPrice(int roomId, double newPrice);                   // Изменение цены номера
                                                                             // Пустая строка перед операциями бронирования
        bool BookRoom(int roomId, const std::string& guestName,              // Создание бронирования номера
                      const std::string& passport,                           // Паспортные данные гостя
                      const std::string& inDate,                             // Дата начала брони
                      const std::string& outDate);                           // Дата окончания брони
                                                                             // Пустая строка перед операциями с гостями и данными
        bool DeleteGuest(int guestId);                                       // Удаление гостя по идентификатору
        void ClearAll();                                                     // Полная очистка данных отеля
                                                                             // Пустая строка перед операциями сохранения и загрузки
        bool SaveToFile(const std::string& filename);                        // Сохранение состояния отеля в файл
        bool LoadFromFile(const std::string& filename);                      // Загрузка состояния отеля из файла
                                                                             // Пустая строка перед методами поиска и получения данных
        std::shared_ptr<Room> FindRoomById(int roomId);                      // Поиск номера по идентификатору
        const std::vector<std::shared_ptr<Building>>& GetBuildings() const { return buildings; } // Получение списка корпусов
        std::vector<std::shared_ptr<Room>> GetAllRooms() const;              // Получение всех номеров отеля
        const std::vector<std::shared_ptr<Guest>>& GetAllGuests() const { return allGuests; } // Получение списка гостей
        std::vector<std::shared_ptr<Booking>> GetAllBookings() const;        // Получение всех бронирований
        double GetTotalRevenue() const { return totalRevenue; }              // Получение накопленной выручки
    };                                                                       // Конец класса Hotel
}                                                                            // Конец пространства имён HotelLogic