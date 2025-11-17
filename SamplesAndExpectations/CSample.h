#pragma once

#include "ISample.h"

// Top defines
#define MY_DEFINE 1
#define MY_DEFINE2 2
// Comment for define 3
#define MY_DEFINE3 3

struct StructOne
{
protected:
    agrint lTestType;

#pragma region Just a h-file pragma test
public:

    // att-id member comment
    TAttId attId;
    TDimValue dimVal;
#pragma endregion // Comment test

public:
    StructOne(const TAttid& inAttId, const TDimValue &inDimVal, agrint lInTestType=0)
    {
        lTestType = lInTestType;
        attid = inAttId;
        dimVal = inDimVal;
    }    
};

// Some more defines
#define MY_DEFINE4 4
#define MY_DEFINE5 5

class CSomeClass
{
    StructOne memberOne;
    int memberTwo;

    // Array of ints
    agrint m_aIntArr1[ARR_SIZE]; // Comment about ARR_SIZE 
 
    public:
        CSomeClass() : memberTwo(33) {
            memberOne.lTestType = 0;
        }

        int GetMemberTwo() const;
};

// Comment for class
class CSample : public ISample
{
private:
    agrint m_value1;

    CString cValue1;
    CString cValue2;
    CString cValue3; 
    CAgrMT* m_pmtReport; //Res/Rate-Reporting (To do: Not touch?)

    // Static member

    static agrint m_iIndex;

    CString PrivateMemberWithBodyInHfile(const TAttId &att_id)
    {
        if (cValue1.IsEmpty()) return _T("");

        return cValue1;
    }

    void TrickyToMatch(const CString& cResTab, 
        const bool& bGetAgeAndTaxNumberFromResTab, /* agrint &oldParameter,*/ CAgrMT* pmtTable);

public:
    CSample();
    ~CSample();

    void MethodOne(const CString& cParam1,
                   const bool &bParam2,
                   CString *pcParam3);

    // Method with body in header file
    bool MethodTwo() { return cValue1 == cValue2; }

private:

    // Comment from .h
    bool MethodP1(const TDimValue& dim1, const agrint& int1, const agrint& int2=0, bool bool1=false);
    bool MethodP5(const TDimValue& dim1, const agrint& int1, const agrint& int2=0);
    bool MethodP4() const;
    bool MethodP2(const TDimValue& dim1, const agrint& int1, const agrint& int2=0, bool bool1=false);
    bool MethodP3(const TDimValue& dim1, const agrint& int1, const agrint& int2);

    bool MethodWithOverloads(const TDimValue& dim1);
    bool MethodWithOverloads(const TDimValue& dim1, const agrint& int1);

    int MethodPrivInl1(const TDimValue& dim1)
    {
        if (dim1.IsEmpty()) 
            return 0;
        
        return 42;
    }

    bool CSample::MethodPrivInl2(const TDimValue& dimPd, const agrint& lLimitHorizon, const agrint& iValue=0, bool bError=false)
    {
        if (dimPd.IsEmpty()) 
            return bError;

        return lLimitHorizon >= iValue;
    }

    int InlineMethodWithOverload(const TDimValue& dim1)
    {
        if (dim1.IsEmpty()) 
            return -1;

        return 100;
    }

    int InlineMethodWithOverload(const TDimValue& dim1, bool bFlag, const CString& cPar = _T("xyz"))
    {
        if (dim1.IsEmpty() || cPar == _T("xyz") || !bFlag)
            return -2;
        return 200;
    }

    int InlineMethodWithOverload(const TDimValue& dim1, bool bFlag, int i = 3)
    {
        if (dim1.IsEmpty()) 
            return -2;

        return 200;
    }
    
    int m_iAnotherPrivateInteger;
};
