#include "Room.h"                                                           // Подключение заголовочного файла с объявлениями номеров
                                                                             // Пустая строка для разделения секций
namespace HotelLogic {                                                       // Начало пространства имён логики отеля
                                                                             // Пустая строка перед реализацией конструктора Room
    Room::Room(int id, std::string number, int cap, double price)            // Реализация конструктора базового номера
        : id(id), roomNumber(number), capacity(cap), basePrice(price), status(RoomStatus::Free) { // Инициализация полей и статуса «Свободен»
    }                                                                        // Конец тела конструктора Room
                                                                             // Пустая строка перед реализацией LuxRoom
    LuxRoom::LuxRoom(int id, std::string number)                             // Реализация конструктора люкс-номера
        : Room(id, number, 2, 5000.0) {                                      // Инициализация с вместимостью 2 и ценой 5000
    }                                                                        // Конец тела конструктора LuxRoom
                                                                             // Пустая строка перед расчётом цены люкса
    double LuxRoom::CalculatePrice(int days) const {                         // Расчёт стоимости проживания в люксе
        return (basePrice * days) * 1.1;                                     // Возврат стоимости с наценкой 10%
    }                                                                        // Конец метода CalculatePrice для LuxRoom
                                                                             // Пустая строка перед реализацией ConferenceRoom
    ConferenceRoom::ConferenceRoom(int id, std::string number)               // Реализация конструктора конференц-зала
        : Room(id, number, 20, 10000.0) {                                    // Инициализация с вместимостью 20 и ценой 10000
    }                                                                        // Конец тела конструктора ConferenceRoom
                                                                             // Пустая строка перед расчётом цены конференц-зала
    double ConferenceRoom::CalculatePrice(int days) const {                  // Расчёт стоимости аренды конференц-зала
        return basePrice * days;                                             // Возврат стоимости без дополнительных наценок
    }                                                                        // Конец метода CalculatePrice для ConferenceRoom
}                                                                            // Конец пространства имён HotelLogic