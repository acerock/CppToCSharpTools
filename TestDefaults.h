#pragma once

class TestDefaults
{
public:
    void Method1(int param1 = 0, bool param2 = false);
    void Method2(const char* str = "", int* ptr = nullptr);
    void Method3(CString name = "DefaultName", agrint value = 42);
};