#pragma once

// Some define
#define IN_INTERFACE_DEF01 1
#define IN_INTERFACE_DEF02 2 // Another define

/* My struct */
typedef struct
{
    bool MyBoolField;
    agrint MyIntField;
} MyStruct;

/* The Interface */
class __declspec(dllexport) ISample
{
public:
    virtual ~ISample(){};
    static ISample* GetInstance();

    virtual void MethodOne(const CString& cParam1,
                           const bool &bParam2,
                           CString *pcParam3) = 0;

    virtual bool MethodTwo() = 0;
};
