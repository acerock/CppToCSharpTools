#pragma once

class TestAllTypes
{
private:
    // Basic C++ types
    int basicInt;
    bool basicBool;
    
    // User-defined types
    agrint customInt;
    CString customString;
    
    // Windows API types
    DWORD winDWord;
    LPSTR winLPStr;
    
    // std:: types
    std::string stdString;
    std::vector<int> stdVector;

public:
    void TestMethod(
        const CString& param1,
        DWORD param2 = 0,
        bool* param3 = nullptr,
        std::string param4 = "default"
    );
    
    LPSTR GetString() const;
    agrint GetValue(const std::vector<int>& vec);
};