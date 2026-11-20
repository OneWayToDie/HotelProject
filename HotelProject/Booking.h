#pragma once                                                                 // Защита от повторного включения заголовочного файла
#include <string>                                                            // Подключение класса строк стандартной библиотеки
                                                                             // Пустая строка для разделения секций
namespace HotelLogic {                                                       // Начало пространства имён логики отеля
                                                                             // Пустая строка перед объявлением класса
    class Booking {                                                          // Класс бронирования номера
    private:                                                                 // Секция приватных полей
        int id;                                                              // Уникальный идентификатор брони
        int roomId;                                                          // Идентификатор забронированного номера
        std::string guestName;                                               // ФИО гостя
        std::string passport;                                                // Паспортные данные гостя
        std::string checkInDate;                                             // Дата заезда в формате yyyy-MM-dd
        std::string checkOutDate;                                            // Дата выезда в формате yyyy-MM-dd
                                                                             // Пустая строка перед публичной секцией
    public:                                                                  // Секция публичных членов класса
        Booking(int id, int roomId, const std::string& name,                 // Конструктор бронирования с основными параметрами
                const std::string& guestPassport,                            // Параметр паспорта гостя
                const std::string& inDate, const std::string& outDate)       // Параметры дат заезда и выезда
            : id(id), roomId(roomId), guestName(name), passport(guestPassport), // Инициализация идентификаторов и данных гостя
              checkInDate(inDate), checkOutDate(outDate) {                   // Инициализация дат проживания
        }                                                                    // Конец тела конструктора
                                                                             // Пустая строка перед геттерами
        int GetId() const { return id; }                                     // Получение идентификатора брони
        int GetRoomId() const { return roomId; }                             // Получение идентификатора номера
        std::string GetGuestName() const { return guestName; }               // Получение ФИО гостя
        std::string GetGuestPassport() const { return passport; }            // Получение паспорта гостя
        std::string GetCheckInDate() const { return checkInDate; }           // Получение даты заезда
        std::string GetCheckOutDate() const { return checkOutDate; }         // Получение даты выезда
                                                                             // Пустая строка перед методом проверки пересечения
        bool Overlaps(const std::string& inDate, const std::string& outDate) const { // Проверка пересечения интервалов дат
            return inDate < checkOutDate && checkInDate < outDate;           // Возврат true при пересечении периодов [inDate, outDate) и [checkInDate, checkOutDate)
        }                                                                    // Конец метода Overlaps
    };                                                                       // Конец объявления класса Booking
}                                                                            // Конец пространства имён HotelLogic
