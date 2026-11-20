#include "Hotel.h"                                                          // Подключение заголовочного файла отеля
#include <iostream>                                                         // Подключение потоков ввода-вывода
#include <fstream>                                                          // Подключение файловых потоков
#include <sstream>                                                          // Подключение строковых потоков
#include <algorithm>                                                        // Подключение алгоритмов стандартной библиотеки
                                                                            // Пустая строка для разделения секций
namespace HotelLogic                                                        // Пространство имён логики отеля
{                                                                           // Начало блока пространства имён
                                                                            // Пустая строка перед реализацией Building
    Building::Building(int id, const std::string& name) : id(id), name(name) {} // Конструктор корпуса с инициализацией полей
                                                                            // Пустая строка перед конструктором Hotel
    Hotel::Hotel() : nextBuildingId(1), nextRoomId(1) {                     // Конструктор отеля с начальными значениями счётчиков
        AddBuilding("Главный корпус");                                      // Создание корпуса по умолчанию
        AddBuilding("Корпус А");                                            // Создание дополнительного корпуса по умолчанию
    }                                                                       // Конец конструктора Hotel
                                                                            // Пустая строка перед добавлением корпуса
    void Hotel::AddBuilding(const std::string& name) {                      // Добавление нового корпуса с автоматическим идентификатором
        buildings.push_back(std::make_shared<Building>(nextBuildingId++, name)); // Создание корпуса и увеличение счётчика
    }                                                                       // Конец метода AddBuilding
                                                                            // Пустая строка перед добавлением корпуса с заданным Id
    void Hotel::AddBuildingWithId(int id, const std::string& name) {        // Добавление корпуса с указанным идентификатором
        buildings.push_back(std::make_shared<Building>(id, name));          // Создание корпуса с переданным идентификатором
        if (id >= nextBuildingId) nextBuildingId = id + 1;                  // Обновление счётчика при необходимости
    }                                                                       // Конец метода AddBuildingWithId
                                                                            // Пустая строка перед добавлением номера с заданным Id
    void Hotel::AddRoomWithId(int id, int buildingId, int roomType,         // Добавление номера с указанным идентификатором
                              const std::string& roomNumber, double price) {// Параметры номера при загрузке данных
        for (auto& b : buildings) {                                         // Перебор всех корпусов
            if (b->GetId() != buildingId) continue;                         // Пропуск корпусов с другим идентификатором
            std::shared_ptr<Room> newRoom;                                  // Указатель на создаваемый номер
            if (roomType == 1) newRoom = std::make_shared<LuxRoom>(id, roomNumber); // Создание люкс-номера
            else if (roomType == 2) newRoom = std::make_shared<ConferenceRoom>(id, roomNumber); // Создание конференц-зала
            else newRoom = std::make_shared<Room>(id, roomNumber, 2, price); // Создание стандартного номера
            newRoom->SetPrice(price);                                       // Установка цены номера
            b->AddRoom(newRoom);                                            // Добавление номера в корпус
            if (id >= nextRoomId) nextRoomId = id + 1;                      // Обновление счётчика номеров
            return;                                                         // Завершение метода после добавления
        }                                                                   // Конец цикла по корпусам
    }                                                                       // Конец метода AddRoomWithId
                                                                            // Пустая строка перед добавлением номера
    void Hotel::AddRoom(int buildingId, int roomType,                       // Добавление нового номера в корпус
                        const std::string& roomNumber, double price) {      // Параметры создаваемого номера
        for (auto& b : buildings) {                                         // Перебор всех корпусов
            if (b->GetId() == buildingId) {                                 // Проверка совпадения идентификатора корпуса
                std::shared_ptr<Room> newRoom;                              // Указатель на создаваемый номер
                if (roomType == 1) newRoom = std::make_shared<LuxRoom>(nextRoomId++, roomNumber); // Создание люкс-номера
                else if (roomType == 2) newRoom = std::make_shared<ConferenceRoom>(nextRoomId++, roomNumber); // Создание конференц-зала
                else newRoom = std::make_shared<Room>(nextRoomId++, roomNumber, 2, price); // Создание стандартного номера
                                                                            // Пустая строка перед установкой цены
                newRoom->SetPrice(price);                                   // Установка фактической цены номера
                                                                            // Пустая строка перед добавлением номера в корпус
                b->AddRoom(newRoom);                                        // Добавление номера в найденный корпус
                return;                                                     // Завершение метода после добавления
            }                                                               // Конец условия совпадения корпуса
        }                                                                   // Конец цикла по корпусам
    }                                                                       // Конец метода AddRoom
                                                                            // Пустая строка перед поиском номера
    std::shared_ptr<Room> Hotel::FindRoomById(int roomId) {                 // Поиск номера по идентификатору
        for (auto& b : buildings) {                                         // Перебор всех корпусов
            for (auto& r : b->GetRooms()) {                                 // Перебор номеров корпуса
                if (r->GetId() == roomId) return r;                         // Возврат найденного номера
            }                                                               // Конец цикла по номерам
        }                                                                   // Конец цикла по корпусам
        return nullptr;                                                     // Возврат пустого указателя, если номер не найден
    }                                                                       // Конец метода FindRoomById
                                                                            // Пустая строка перед получением всех номеров
    std::vector<std::shared_ptr<Room>> Hotel::GetAllRooms() const {         // Получение списка всех номеров отеля
        std::vector<std::shared_ptr<Room>> all;                             // Вектор для накопления номеров
        for (auto& b : buildings) {                                         // Перебор всех корпусов
            for (auto& r : b->GetRooms()) {                                 // Перебор номеров корпуса
                all.push_back(r);                                           // Добавление номера в общий список
            }                                                               // Конец цикла по номерам
        }                                                                   // Конец цикла по корпусам
        return all;                                                         // Возврат общего списка номеров
    }                                                                       // Конец метода GetAllRooms
                                                                            // Пустая строка перед заселением гостя
    bool Hotel::CheckIn(int roomId, const std::string& guestName,           // Заселение гостя в номер
                        const std::string& passport,                        // Паспортные данные гостя
                        const std::string& inDate,                          // Дата заезда
                        const std::string& outDate, int days) {             // Дата выезда и количество дней
        auto room = FindRoomById(roomId);                                   // Поиск номера по идентификатору
        if (room && room->GetStatus() == RoomStatus::Free) {                // Проверка наличия свободного номера
            room->SetStatus(RoomStatus::Occupied);                          // Установка статуса «Занят»
            room->SetGuest(guestName, passport);                            // Сохранение данных гостя в номере
            room->SetBookingDates(inDate, outDate, days);                   // Сохранение дат и длительности проживания
                                                                            // Пустая строка перед удалением собственной брони
            room->RemoveBookingByPassport(passport);                        // Удаление брони гостя, если он заселяется по ней
                                                                            // Пустая строка перед проверкой гостя в базе
            bool exists = false;                                            // Флаг наличия гостя в базе
            for (auto& g : allGuests) {                                     // Перебор всех зарегистрированных гостей
                if (g->passport == passport) { exists = true; break; }      // Проверка совпадения паспорта
            }                                                               // Конец цикла по гостям
            if (!exists) {                                                  // Проверка необходимости добавить гостя
                allGuests.push_back(std::make_shared<Guest>(nextGuestId++, guestName, passport)); // Добавление нового гостя
            }                                                               // Конец условия добавления гостя
                                                                            // Пустая строка перед возвратом результата
            return true;                                                    // Возврат успешного результата заселения
        }                                                                   // Конец условия успешного заселения
        return false;                                                       // Возврат неуспешного результата
    }                                                                       // Конец метода CheckIn
                                                                            // Пустая строка перед выселением гостя
    double Hotel::CheckOut(int roomId) {                                    // Выселение гостя из номера
        auto room = FindRoomById(roomId);                                   // Поиск номера по идентификатору
        if (room && room->GetStatus() == RoomStatus::Occupied) {            // Проверка, что номер занят
            room->SetStatus(RoomStatus::Cleaning);                          // Перевод номера в статус «На уборке»
                                                                            // Пустая строка перед расчётом суммы
            int daysStayed = room->GetBookedDays();                         // Получение количества дней проживания
            if (daysStayed <= 0) daysStayed = 1;                            // Минимальная длительность проживания — 1 день
                                                                            // Пустая строка перед расчётом выручки
            double total = room->CalculatePrice(daysStayed);                // Расчёт итоговой стоимости проживания
            totalRevenue += total;                                          // Добавление суммы к накопленной выручке
            room->SetGuest("", "");                                         // Очистка данных гостя в номере
            return total;                                                   // Возврат суммы к оплате
        }                                                                   // Конец условия успешного выселения
        return 0.0;                                                         // Возврат нуля при неудачном выселении
    }                                                                       // Конец метода CheckOut
                                                                            // Пустая строка перед завершением уборки
    void Hotel::FinishCleaning(int roomId) {                                // Завершение уборки номера
        auto room = FindRoomById(roomId);                                   // Поиск номера по идентификатору
        if (room && room->GetStatus() == RoomStatus::Cleaning) {            // Проверка, что номер находится на уборке
            room->SetStatus(RoomStatus::Free);                              // Перевод номера в статус «Свободен»
        }                                                                   // Конец условия завершения уборки
    }                                                                       // Конец метода FinishCleaning
                                                                            // Пустая строка перед изменением цены
    void Hotel::UpdateRoomPrice(int roomId, double newPrice) {              // Обновление цены номера
        auto room = FindRoomById(roomId);                                   // Поиск номера по идентификатору
        if (room) room->SetPrice(newPrice);                                 // Установка новой цены при наличии номера
    }                                                                       // Конец метода UpdateRoomPrice
                                                                            // Пустая строка перед бронированием
    bool Hotel::BookRoom(int roomId, const std::string& guestName,          // Создание бронирования номера
                         const std::string& passport,                       // Паспортные данные гостя
                         const std::string& inDate,                         // Дата заезда
                         const std::string& outDate) {                      // Дата выезда
        auto room = FindRoomById(roomId);                                   // Поиск номера по идентификатору
        if (!room) return false;                                            // Возврат false, если номер не найден
        if (inDate.empty() || outDate.empty() || inDate >= outDate) return false; // Проверка корректности дат
                                                                            // Пустая строка перед проверкой конфликтов
        if (room->HasBookingConflict(inDate, outDate)) return false;        // Проверка пересечения с существующими бронями
                                                                            // Пустая строка перед созданием брони
        auto booking = std::make_shared<Booking>(nextBookingId++, roomId, guestName, passport, inDate, outDate); // Создание объекта брони
        room->AddBooking(booking);                                          // Добавление брони в номер
                                                                            // Пустая строка перед добавлением гостя
        bool exists = false;                                                // Флаг наличия гостя в базе
        for (auto& g : allGuests) {                                         // Перебор всех зарегистрированных гостей
            if (g->passport == passport) { exists = true; break; }          // Проверка совпадения паспорта
        }                                                                   // Конец цикла по гостям
        if (!exists) {                                                      // Проверка необходимости добавить гостя
            allGuests.push_back(std::make_shared<Guest>(nextGuestId++, guestName, passport)); // Добавление нового гостя
        }                                                                   // Конец условия добавления гостя
                                                                            // Пустая строка перед возвратом результата
        return true;                                                        // Возврат успешного результата бронирования
    }                                                                       // Конец метода BookRoom
                                                                            // Пустая строка перед удалением гостя
    bool Hotel::DeleteGuest(int guestId) {                                  // Удаление гостя по идентификатору
        size_t before = allGuests.size();                                   // Сохранение количества гостей до удаления
        allGuests.erase(                                                    // Удаление гостя из контейнера
            std::remove_if(allGuests.begin(), allGuests.end(),              // Поиск гостя по идентификатору
                [guestId](const std::shared_ptr<Guest>& guest) {            // Лямбда-функция сравнения идентификатора
                    return guest && guest->id == guestId;                   // Проверка совпадения идентификатора
                }),                                                         // Конец лямбда-функции и remove_if
            allGuests.end());                                               // Передача конечного итератора для erase
        return allGuests.size() != before;                                  // Возврат признака успешного удаления
    }                                                                       // Конец метода DeleteGuest
                                                                            // Пустая строка перед очисткой данных
    void Hotel::ClearAll() {                                                // Полная очистка данных отеля
        buildings.clear();                                                  // Очистка списка корпусов
        allGuests.clear();                                                  // Очистка списка гостей
        nextBuildingId = 1;                                                 // Сброс счётчика корпусов
        nextRoomId = 1;                                                     // Сброс счётчика номеров
        nextGuestId = 1;                                                    // Сброс счётчика гостей
        nextBookingId = 1;                                                  // Сброс счётчика бронирований
        totalRevenue = 0.0;                                                 // Сброс накопленной выручки
                                                                            // Пустая строка перед созданием корпусов по умолчанию
        AddBuilding("Главный корпус");                                      // Создание корпуса по умолчанию
        AddBuilding("Корпус А");                                            // Создание дополнительного корпуса по умолчанию
    }                                                                       // Конец метода ClearAll
                                                                            // Пустая строка перед получением всех бронирований
    std::vector<std::shared_ptr<Booking>> Hotel::GetAllBookings() const {   // Получение всех бронирований отеля
        std::vector<std::shared_ptr<Booking>> all;                          // Вектор для накопления бронирований
        for (auto& b : buildings) {                                         // Перебор всех корпусов
            for (auto& r : b->GetRooms()) {                                 // Перебор номеров корпуса
                for (auto& booking : r->GetBookings()) {                    // Перебор бронирований номера
                    all.push_back(booking);                                 // Добавление брони в общий список
                }                                                           // Конец цикла по бронированиям
            }                                                               // Конец цикла по номерам
        }                                                                   // Конец цикла по корпусам
        return all;                                                         // Возврат общего списка бронирований
    }                                                                       // Конец метода GetAllBookings
                                                                            // Пустая строка перед сохранением в файл
    bool Hotel::SaveToFile(const std::string& filename) {                   // Сохранение состояния отеля в файл
        std::ofstream out(filename);                                        // Открытие файла для записи
        if (!out.is_open()) return false;                                   // Возврат false при ошибке открытия файла
                                                                            // Пустая строка перед записью корпусов и номеров
        for (auto& b : buildings) {                                         // Перебор всех корпусов
            out << "BUILDING\t" << b->GetId() << "\t" << b->GetName() << "\n"; // Запись строки корпуса
                                                                            // Пустая строка перед записью номеров корпуса
            for (auto& r : b->GetRooms()) {                                 // Перебор номеров корпуса
                out << "ROOM\t" << r->GetId() << "\t" << b->GetId() << "\t" // Запись идентификаторов номера и корпуса
                    << r->GetNumber() << "\t" << (int)r->GetStatus() << "\t" // Запись номера и статуса
                    << r->GetTypeName() << "\t" << r->GetBasePrice() << "\t" // Запись типа и базовой цены
                    << r->GetBookedDays() << "\t"                           // Запись количества дней проживания
                    << r->GetGuestName() << "\t" << r->GetGuestPassport() << "\t" // Запись данных гостя
                    << r->GetCheckInDate() << "\t" << r->GetCheckOutDate() << "\n"; // Запись дат проживания
            }                                                               // Конец цикла по номерам
        }                                                                   // Конец цикла по корпусам
                                                                            // Пустая строка перед записью гостей
        for (auto& g : allGuests) {                                         // Перебор всех гостей
            out << "GUEST\t" << g->id << "\t" << g->fullName << "\t" << g->passport << "\n"; // Запись строки гостя
        }                                                                   // Конец цикла по гостям
                                                                            // Пустая строка перед записью бронирований
        for (auto& b : buildings) {                                         // Перебор всех корпусов
            for (auto& r : b->GetRooms()) {                                 // Перебор номеров корпуса
                for (auto& booking : r->GetBookings()) {                    // Перебор бронирований номера
                    out << "BOOKING\t" << booking->GetId() << "\t" << booking->GetRoomId() << "\t" // Запись идентификаторов брони и номера
                        << booking->GetGuestName() << "\t" << booking->GetGuestPassport() << "\t" // Запись данных гостя
                        << booking->GetCheckInDate() << "\t" << booking->GetCheckOutDate() << "\n"; // Запись дат брони
                }                                                           // Конец цикла по бронированиям
            }                                                               // Конец цикла по номерам
        }                                                                   // Конец цикла по корпусам
                                                                            // Пустая строка перед записью статистики
        out << "STATS\t" << totalRevenue << "\n";                           // Запись накопленной выручки
        out.close();                                                        // Закрытие файла
        return true;                                                        // Возврат успешного результата сохранения
    }                                                                       // Конец метода SaveToFile
                                                                            // Пустая строка перед загрузкой из файла
    bool Hotel::LoadFromFile(const std::string& filename) {                 // Загрузка состояния отеля из файла
        std::ifstream in(filename);                                         // Открытие файла для чтения
        if (!in.is_open()) return false;                                   // Возврат false при ошибке открытия файла
                                                                            // Пустая строка перед сбросом текущих данных
        buildings.clear();                                                  // Очистка списка корпусов
        allGuests.clear();                                                  // Очистка списка гостей
        nextBuildingId = 1;                                                 // Сброс счётчика корпусов
        nextRoomId = 1;                                                     // Сброс счётчика номеров
        nextGuestId = 1;                                                    // Сброс счётчика гостей
        nextBookingId = 1;                                                  // Сброс счётчика бронирований
        totalRevenue = 0.0;                                                 // Сброс накопленной выручки
                                                                            // Пустая строка перед чтением строк файла
        std::string line;                                                   // Буфер для текущей строки файла
        while (std::getline(in, line)) {                                    // Чтение файла построчно
            std::stringstream ss(line);                                     // Создание строкового потока для разбора
            std::string type;                                               // Тип записи в строке
            std::getline(ss, type, '\t');                                   // Чтение типа записи
                                                                            // Пустая строка перед разбором корпуса
            if (type == "BUILDING") {                                       // Обработка записи корпуса
                std::string idStr, name;                                    // Буферы для идентификатора и названия
                std::getline(ss, idStr, '\t');                              // Чтение идентификатора корпуса
                std::getline(ss, name, '\t');                               // Чтение названия корпуса
                                                                            // Пустая строка перед преобразованием идентификатора
                int id = 0;                                                 // Переменная для числового идентификатора
                try { id = std::stoi(idStr); }                              // Преобразование строки в число
                catch (...) { continue; }                                   // Пропуск записи при ошибке преобразования
                                                                            // Пустая строка перед добавлением корпуса
                AddBuildingWithId(id, name);                                // Добавление корпуса с сохранённым идентификатором
            }                                                               // Конец обработки записи корпуса
            else if (type == "ROOM") {                                      // Обработка записи номера
                std::string idStr, bIdStr, number, statusStr, typeName, priceStr, daysStr, guest, passport, inDate, outDate; // Буферы полей номера
                std::getline(ss, idStr, '\t');                              // Чтение идентификатора номера
                std::getline(ss, bIdStr, '\t');                             // Чтение идентификатора корпуса
                std::getline(ss, number, '\t');                             // Чтение номера комнаты
                std::getline(ss, statusStr, '\t');                          // Чтение статуса номера
                std::getline(ss, typeName, '\t');                           // Чтение названия типа номера
                std::getline(ss, priceStr, '\t');                           // Чтение базовой цены
                std::getline(ss, daysStr, '\t');                            // Чтение количества дней проживания
                std::getline(ss, guest, '\t');                              // Чтение имени гостя
                std::getline(ss, passport, '\t');                           // Чтение паспорта гостя
                std::getline(ss, inDate, '\t');                             // Чтение даты заезда
                std::getline(ss, outDate, '\t');                            // Чтение даты выезда
                                                                            // Пустая строка перед преобразованием числовых полей
                int id = 0, bId = 0, status = 0;                            // Переменные для числовых значений
                try {                                                       // Попытка преобразования обязательных чисел
                    id = std::stoi(idStr);                                  // Преобразование идентификатора номера
                    bId = std::stoi(bIdStr);                                // Преобразование идентификатора корпуса
                    status = std::stoi(statusStr);                          // Преобразование статуса номера
                }                                                           // Конец блока try
                catch (...) { continue; }                                   // Пропуск записи при ошибке преобразования
                                                                            // Пустая строка перед преобразованием цены и дней
                double price = 2000.0;                                      // Значение цены по умолчанию
                if (!priceStr.empty()) { try { price = std::stod(priceStr); } catch (...) {} } // Преобразование цены при наличии значения
                                                                            // Пустая строка перед преобразованием дней
                int days = 0;                                               // Количество дней проживания по умолчанию
                if (!daysStr.empty()) { try { days = std::stoi(daysStr); } catch (...) {} } // Преобразование дней при наличии значения
                                                                            // Пустая строка перед определением типа номера
                int roomType = (typeName == "Люкс") ? 1 : (typeName == "Конференц-зал" ? 2 : 0); // Определение числового типа номера
                AddRoomWithId(id, bId, roomType, number, price);            // Добавление номера с восстановленным идентификатором
                                                                            // Пустая строка перед восстановлением состояния номера
                auto room = FindRoomById(id);                               // Поиск добавленного номера
                if (room) {                                                 // Проверка успешного добавления номера
                    room->SetStatus((RoomStatus)status);                    // Восстановление статуса номера
                    room->SetGuest(guest, passport);                        // Восстановление данных гостя
                    room->SetBookingDates(inDate, outDate, days);           // Восстановление дат и длительности проживания
                }                                                           // Конец условия восстановления номера
            }                                                               // Конец обработки записи номера
            else if (type == "GUEST") {                                     // Обработка записи гостя
                std::string idStr, name, passport;                          // Буферы для полей гостя
                std::getline(ss, idStr, '\t');                              // Чтение идентификатора гостя
                std::getline(ss, name, '\t');                               // Чтение имени гостя
                std::getline(ss, passport, '\t');                           // Чтение паспорта гостя
                                                                            // Пустая строка перед преобразованием идентификатора
                int id = 0;                                                 // Переменная для числового идентификатора
                try { id = std::stoi(idStr); }                              // Преобразование строки в число
                catch (...) { continue; }                                   // Пропуск записи при ошибке преобразования
                                                                            // Пустая строка перед добавлением гостя
                allGuests.push_back(std::make_shared<Guest>(id, name, passport)); // Добавление гостя в список
                if (id >= nextGuestId) nextGuestId = id + 1;                // Обновление счётчика гостей
            }                                                               // Конец обработки записи гостя
            else if (type == "STATS") {                                     // Обработка записи статистики
                std::string revenueStr;                                     // Буфер для значения выручки
                std::getline(ss, revenueStr, '\t');                         // Чтение значения выручки
                if (!revenueStr.empty()) {                                  // Проверка наличия значения
                    try { totalRevenue = std::stod(revenueStr); } catch (...) {} // Преобразование строки в число
                }                                                           // Конец условия наличия значения
            }                                                               // Конец обработки записи статистики
            else if (type == "BOOKING") {                                   // Обработка записи бронирования
                std::string idStr, roomIdStr, name, passport, inDate, outDate; // Буферы для полей бронирования
                std::getline(ss, idStr, '\t');                              // Чтение идентификатора брони
                std::getline(ss, roomIdStr, '\t');                          // Чтение идентификатора номера
                std::getline(ss, name, '\t');                               // Чтение имени гостя
                std::getline(ss, passport, '\t');                           // Чтение паспорта гостя
                std::getline(ss, inDate, '\t');                             // Чтение даты заезда
                std::getline(ss, outDate, '\t');                            // Чтение даты выезда
                                                                            // Пустая строка перед преобразованием идентификаторов
                int id = 0, roomId = 0;                                     // Переменные для числовых идентификаторов
                try {                                                       // Попытка преобразования идентификаторов
                    id = std::stoi(idStr);                                  // Преобразование идентификатора брони
                    roomId = std::stoi(roomIdStr);                          // Преобразование идентификатора номера
                }                                                           // Конец блока try
                catch (...) { continue; }                                   // Пропуск записи при ошибке преобразования
                                                                            // Пустая строка перед добавлением брони в номер
                auto room = FindRoomById(roomId);                           // Поиск номера по идентификатору
                if (room) {                                                 // Проверка наличия номера
                    room->AddBooking(std::make_shared<Booking>(id, roomId, name, passport, inDate, outDate)); // Добавление брони в номер
                }                                                           // Конец условия наличия номера
                if (id >= nextBookingId) nextBookingId = id + 1;            // Обновление счётчика бронирований
            }                                                               // Конец обработки записи бронирования
        }                                                                   // Конец цикла чтения файла
        in.close();                                                         // Закрытие файла
        return true;                                                        // Возврат успешного результата загрузки
    }                                                                       // Конец метода LoadFromFile
}                                                                           // Конец пространства имён HotelLogic