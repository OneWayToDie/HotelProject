#pragma once
#include <string>

namespace HotelLogic 
{
    class Guest {
    public:
        int id;
        std::string fullName;
        std::string passport;

        Guest(int id, const std::string& name, const std::string& passport)
            : id(id), fullName(name), passport(passport) {
        }
    };
}
