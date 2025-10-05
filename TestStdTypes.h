#pragma once

class TestStdTypes
{
public:
    std::string myString;
    std::vector<int> myVector;

    void MyMethod(std::string param1, const std::vector<double>& param2);
    std::string GetString() const;
};