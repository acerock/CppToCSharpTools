#pragma once

class TestAllBasicTypes
{
private:
    // Basic signed types
    char charVar;
    short shortVar;
    int intVar;
    long longVar;
    
    // Basic unsigned types  
    unsigned char ucharVar;
    unsigned short ushortVar;
    unsigned int uintVar;
    unsigned long ulongVar;
    
    // Floating point types
    float floatVar;
    double doubleVar;
    
    // Size and special types
    size_t sizeVar;
    
    // User-defined types
    agrint agrintVar;
    CString cstringVar;
    
    // Windows API types
    DWORD dwordVar;
    LPSTR lpstrVar;
    
    // std:: types
    std::string stdStringVar;

public:
    void TestMethod(
        unsigned char param1,
        size_t param2 = 0,
        const unsigned long& param3 = 100,
        DWORD* param4 = nullptr
    );
};