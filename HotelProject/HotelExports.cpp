#include "Hotel.h"                                                          // Подключение заголовочного файла отеля
#include <cstring>                                                          // Подключение функций работы со строками C
#include <vector>                                                           // Подключение контейнера вектор
                                                                            // Пустая строка для разделения секций
using namespace HotelLogic;                                                 // Использование пространства имён логики отеля
                                                                            // Пустая строка перед настройкой упаковки структур
#pragma pack(push, 1)                                                       // Установка выравнивания структур в 1 байт
struct CRoom {                                                              // C-совместимая структура номера
    int id;                                                                 // Идентификатор номера
    int buildingId;                                                         // Идентификатор корпуса
    char number[20];                                                        // Отображаемый номер комнаты
    int capacity;                                                           // Вместимость номера
    double basePrice;                                                       // Базовая цена за сутки
    int status;                                                             // Статус номера
    int type;                                                               // Тип номера
    char guestName[100];                                                    // Имя текущего гостя
    char guestPassport[50];                                                 // Паспорт текущего гостя
    char checkInDate[20];                                                   // Дата заезда
    char checkOutDate[20];                                                  // Дата выезда
};                                                                          // Конец структуры CRoom
                                                                            // Пустая строка перед структурой корпуса
struct CBuilding {                                                          // C-совместимая структура корпуса
    int id;                                                                 // Идентификатор корпуса
    char name[100];                                                         // Название корпуса
};                                                                          // Конец структуры CBuilding
                                                                            // Пустая строка перед структурой гостя
struct CGuest {                                                             // C-совместимая структура гостя
    int id;                                                                 // Идентификатор гостя
    char fullName[100];                                                     // Полное имя гостя
    char passport[50];                                                      // Паспортные данные гостя
};                                                                          // Конец структуры CGuest
                                                                            // Пустая строка перед структурой бронирования
struct CBooking {                                                           // C-совместимая структура бронирования
    int id;                                                                 // Идентификатор бронирования
    int roomId;                                                             // Идентификатор номера
    char guestName[100];                                                    // Имя гостя
    char passport[50];                                                      // Паспорт гостя
    char checkInDate[20];                                                   // Дата заезда
    char checkOutDate[20];                                                  // Дата выезда
};                                                                          // Конец структуры CBooking
#pragma pack(pop)                                                           // Восстановление предыдущего выравнивания структур
                                                                            // Пустая строка перед вспомогательным пространством имён
namespace {                                                                 // Анонимное пространство имён для внутренних функций
    void CopyStr(char* dest, size_t size, const std::string& src) {        // Копирование строки std::string в буфер C
        strncpy_s(dest, size, src.c_str(), size - 1);                       // Безопасное копирование строки с ограничением размера
    }                                                                       // Конец функции CopyStr
}                                                                           // Конец анонимного пространства имён
                                                                            // Пустая строка перед экспортируемыми функциями
extern "C" {                                                                // Блок функций с C-соглашением о вызовах
    __declspec(dllexport) void* Hotel_Create() {                            // Создание экземпляра отеля
        return new Hotel();                                                 // Возврат указателя на новый объект Hotel
    }                                                                       // Конец функции Hotel_Create
                                                                            // Пустая строка перед уничтожением отеля
    __declspec(dllexport) void Hotel_Destroy(void* ptr) {                   // Уничтожение экземпляра отеля
        if (ptr) delete static_cast<Hotel*>(ptr);                           // Освобождение памяти при ненулевом указателе
    }                                                                       // Конец функции Hotel_Destroy
                                                                            // Пустая строка перед добавлением корпуса
    __declspec(dllexport) void Hotel_AddBuilding(void* ptr, const char* name) { // Добавление корпуса в отель
        if (ptr) static_cast<Hotel*>(ptr)->AddBuilding(name);               // Вызов метода добавления корпуса
    }                                                                       // Конец функции Hotel_AddBuilding
                                                                            // Пустая строка перед добавлением номера
    __declspec(dllexport) void Hotel_AddRoom(void* ptr, int buildingId, int type, const char* number, double price) { // Добавление номера в отель
        if (ptr) static_cast<Hotel*>(ptr)->AddRoom(buildingId, type, number, price); // Вызов метода добавления номера
    }                                                                       // Конец функции Hotel_AddRoom
                                                                            // Пустая строка перед заселением гостя
    __declspec(dllexport) bool Hotel_CheckIn(void* ptr, int roomId, const char* name, const char* passport, const char* inDate, const char* outDate, int days) { // Заселение гостя
        if (!ptr) return false;                                             // Возврат false при пустом указателе
        return static_cast<Hotel*>(ptr)->CheckIn(roomId, name, passport, inDate, outDate, days); // Вызов метода заселения
    }                                                                       // Конец функции Hotel_CheckIn
                                                                            // Пустая строка перед выселением гостя
    __declspec(dllexport) double Hotel_CheckOut(void* ptr, int roomId) {    // Выселение гостя из номера
        if (!ptr) return 0.0;                                               // Возврат нуля при пустом указателе
        return static_cast<Hotel*>(ptr)->CheckOut(roomId);                  // Вызов метода выселения
    }                                                                       // Конец функции Hotel_CheckOut
                                                                            // Пустая строка перед завершением уборки
    __declspec(dllexport) void Hotel_FinishCleaning(void* ptr, int roomId) {// Завершение уборки номера
        if (ptr) static_cast<Hotel*>(ptr)->FinishCleaning(roomId);          // Вызов метода завершения уборки
    }                                                                       // Конец функции Hotel_FinishCleaning
                                                                            // Пустая строка перед обновлением цены
    __declspec(dllexport) void Hotel_UpdatePrice(void* ptr, int roomId, double newPrice) { // Обновление цены номера
        if (ptr) static_cast<Hotel*>(ptr)->UpdateRoomPrice(roomId, newPrice); // Вызов метода изменения цены
    }                                                                       // Конец функции Hotel_UpdatePrice
                                                                            // Пустая строка перед получением всех номеров
    __declspec(dllexport) int Hotel_GetAllRooms(void* ptr, CRoom* buffer, int maxSize) { // Получение списка номеров
        if (!ptr) return 0;                                                 // Возврат нуля при пустом указателе
        Hotel* hotel = static_cast<Hotel*>(ptr);                            // Приведение указателя к типу Hotel
                                                                            // Пустая строка перед заполнением буфера
        int index = 0;                                                      // Индекс текущей записи в буфере
        for (auto& b : hotel->GetBuildings()) {                             // Перебор всех корпусов
            for (auto& r : b->GetRooms()) {                                 // Перебор номеров корпуса
                if (index >= maxSize) break;                                // Прерывание при заполнении буфера
                                                                            // Пустая строка перед копированием полей номера
                buffer[index].id = r->GetId();                              // Копирование идентификатора номера
                buffer[index].buildingId = b->GetId();                      // Копирование идентификатора корпуса
                CopyStr(buffer[index].number, sizeof(buffer[index].number), r->GetNumber()); // Копирование номера комнаты
                buffer[index].capacity = r->GetCapacity();                  // Копирование вместимости номера
                buffer[index].basePrice = r->GetBasePrice();                // Копирование базовой цены
                buffer[index].status = (int)r->GetStatus();                 // Копирование статуса номера
                                                                            // Пустая строка перед определением типа номера
                std::string typeName = r->GetTypeName();                    // Получение названия типа номера
                if (typeName == "Люкс") buffer[index].type = 1;             // Установка типа «Люкс»
                else if (typeName == "Конференц-зал") buffer[index].type = 2; // Установка типа «Конференц-зал»
                else buffer[index].type = 0;                                // Установка типа «Стандарт»
                                                                            // Пустая строка перед копированием данных гостя
                CopyStr(buffer[index].guestName, sizeof(buffer[index].guestName), r->GetGuestName()); // Копирование имени гостя
                CopyStr(buffer[index].guestPassport, sizeof(buffer[index].guestPassport), r->GetGuestPassport()); // Копирование паспорта гостя
                                                                            // Пустая строка перед копированием дат
                CopyStr(buffer[index].checkInDate, sizeof(buffer[index].checkInDate), r->GetCheckInDate()); // Копирование даты заезда
                CopyStr(buffer[index].checkOutDate, sizeof(buffer[index].checkOutDate), r->GetCheckOutDate()); // Копирование даты выезда
                                                                            // Пустая строка перед переходом к следующей записи
                index++;                                                    // Увеличение индекса буфера
            }                                                               // Конец цикла по номерам
        }                                                                   // Конец цикла по корпусам
        return index;                                                       // Возврат количества записанных номеров
    }                                                                       // Конец функции Hotel_GetAllRooms
                                                                            // Пустая строка перед получением всех корпусов
    __declspec(dllexport) int Hotel_GetAllBuildings(void* ptr, CBuilding* buffer, int maxSize) { // Получение списка корпусов
        if (!ptr) return 0;                                                 // Возврат нуля при пустом указателе
        Hotel* hotel = static_cast<Hotel*>(ptr);                            // Приведение указателя к типу Hotel
        auto bldgs = hotel->GetBuildings();                                 // Получение списка корпусов
                                                                            // Пустая строка перед ограничением количества записей
        int count = (int)bldgs.size();                                      // Определение количества корпусов
        if (count > maxSize) count = maxSize;                               // Ограничение количества размером буфера
                                                                            // Пустая строка перед заполнением буфера
        for (int i = 0; i < count; i++) {                                   // Перебор корпусов в пределах буфера
            buffer[i].id = bldgs[i]->GetId();                               // Копирование идентификатора корпуса
            CopyStr(buffer[i].name, sizeof(buffer[i].name), bldgs[i]->GetName()); // Копирование названия корпуса
        }                                                                   // Конец цикла по корпусам
        return count;                                                       // Возврат количества записанных корпусов
    }                                                                       // Конец функции Hotel_GetAllBuildings
                                                                            // Пустая строка перед сохранением данных
    __declspec(dllexport) bool Hotel_Save(void* ptr, const char* filename) {// Сохранение состояния отеля
        if (!ptr) return false;                                             // Возврат false при пустом указателе
        return static_cast<Hotel*>(ptr)->SaveToFile(filename);              // Вызов метода сохранения в файл
    }                                                                       // Конец функции Hotel_Save
                                                                            // Пустая строка перед загрузкой данных
    __declspec(dllexport) bool Hotel_Load(void* ptr, const char* filename) {// Загрузка состояния отеля
        if (!ptr) return false;                                             // Возврат false при пустом указателе
        return static_cast<Hotel*>(ptr)->LoadFromFile(filename);            // Вызов метода загрузки из файла
    }                                                                       // Конец функции Hotel_Load
                                                                            // Пустая строка перед получением всех гостей
    __declspec(dllexport) int Hotel_GetAllGuests(void* ptr, CGuest* buffer, int maxSize) { // Получение списка гостей
        if (!ptr) return 0;                                                 // Возврат нуля при пустом указателе
        Hotel* hotel = static_cast<Hotel*>(ptr);                            // Приведение указателя к типу Hotel
        auto guests = hotel->GetAllGuests();                                // Получение списка гостей
        int count = (int)guests.size();                                     // Определение количества гостей
        if (count > maxSize) count = maxSize;                               // Ограничение количества размером буфера
                                                                            // Пустая строка перед заполнением буфера
        for (int i = 0; i < count; i++) {                                   // Перебор гостей в пределах буфера
            buffer[i].id = guests[i]->id;                                   // Копирование идентификатора гостя
            CopyStr(buffer[i].fullName, sizeof(buffer[i].fullName), guests[i]->fullName); // Копирование имени гостя
            CopyStr(buffer[i].passport, sizeof(buffer[i].passport), guests[i]->passport); // Копирование паспорта гостя
        }                                                                   // Конец цикла по гостям
        return count;                                                       // Возврат количества записанных гостей
    }                                                                       // Конец функции Hotel_GetAllGuests
                                                                            // Пустая строка перед очисткой данных
    __declspec(dllexport) void Hotel_ClearAll(void* ptr) {                  // Полная очистка данных отеля
        if (!ptr) return;                                                   // Выход при пустом указателе
        static_cast<Hotel*>(ptr)->ClearAll();                               // Вызов метода очистки данных
    }                                                                       // Конец функции Hotel_ClearAll
                                                                            // Пустая строка перед удалением гостя
    __declspec(dllexport) bool Hotel_DeleteGuest(void* ptr, int guestId) {  // Удаление гостя по идентификатору
        if (!ptr) return false;                                             // Возврат false при пустом указателе
        return static_cast<Hotel*>(ptr)->DeleteGuest(guestId);              // Вызов метода удаления гостя
    }                                                                       // Конец функции Hotel_DeleteGuest
                                                                            // Пустая строка перед получением выручки
    __declspec(dllexport) double Hotel_GetTotalRevenue(void* ptr) {         // Получение накопленной выручки
        if (!ptr) return 0.0;                                               // Возврат нуля при пустом указателе
        return static_cast<Hotel*>(ptr)->GetTotalRevenue();                 // Вызов метода получения выручки
    }                                                                       // Конец функции Hotel_GetTotalRevenue
                                                                            // Пустая строка перед бронированием номера
    __declspec(dllexport) bool Hotel_BookRoom(void* ptr, int roomId, const char* name, const char* passport, const char* inDate, const char* outDate) { // Создание бронирования
        if (!ptr) return false;                                             // Возврат false при пустом указателе
        return static_cast<Hotel*>(ptr)->BookRoom(roomId, name, passport, inDate, outDate); // Вызов метода бронирования
    }                                                                       // Конец функции Hotel_BookRoom
                                                                            // Пустая строка перед получением всех бронирований
    __declspec(dllexport) int Hotel_GetAllBookings(void* ptr, CBooking* buffer, int maxSize) { // Получение списка бронирований
        if (!ptr) return 0;                                                 // Возврат нуля при пустом указателе
        Hotel* hotel = static_cast<Hotel*>(ptr);                            // Приведение указателя к типу Hotel
        auto bookings = hotel->GetAllBookings();                            // Получение списка бронирований
        int count = (int)bookings.size();                                   // Определение количества бронирований
        if (count > maxSize) count = maxSize;                               // Ограничение количества размером буфера
                                                                            // Пустая строка перед заполнением буфера
        for (int i = 0; i < count; i++) {                                   // Перебор бронирований в пределах буфера
            auto b = bookings[i];                                           // Получение текущего бронирования
            buffer[i].id = b->GetId();                                      // Копирование идентификатора брони
            buffer[i].roomId = b->GetRoomId();                              // Копирование идентификатора номера
            CopyStr(buffer[i].guestName, sizeof(buffer[i].guestName), b->GetGuestName()); // Копирование имени гостя
            CopyStr(buffer[i].passport, sizeof(buffer[i].passport), b->GetGuestPassport()); // Копирование паспорта гостя
            CopyStr(buffer[i].checkInDate, sizeof(buffer[i].checkInDate), b->GetCheckInDate()); // Копирование даты заезда
            CopyStr(buffer[i].checkOutDate, sizeof(buffer[i].checkOutDate), b->GetCheckOutDate()); // Копирование даты выезда
        }                                                                   // Конец цикла по бронированиям
        return count;                                                       // Возврат количества записанных бронирований
    }                                                                       // Конец функции Hotel_GetAllBookings
}                                                                           // Конец блока extern "C"