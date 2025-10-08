#include "StdAfx.h"
#include "CSample.h"

/* DEFINES IN CPP*/
// Also cpp files can have defines
#define CPP_DEFINE 10
#define CPP_DEFINE2 20 
// Comment for cpp define 3
#define CPP_DEFINE3 30 

ISample* ISample::GetInstance()
{
    CSample* pSample = new CSample();
    return pSample;
};

int CSomeClass::GetMemberTwo() const
{
    /* Sample method body */
    return memberTwo;
}
     
CSample::m_iIndex = -1;

CSample::CSample()
{
    m_value1 = 0;
 
    cValue1 = _T("ABC");
    cValue2 = _T("DEF");
    cValue3 = _T("GHI");
};

#define CPP_DEFINE4 40

CSample::~CSample()
{
    // Cleanup code here
};

void CSample::MethodOne(const CString& cParam1,
                   const bool &bParam2,
                   CString *pcParam3)
{
    // Implementation of MethodOne
}

// Comment from .cpp
bool CSample::MethodP1(const TDimValue& dimPd, const agrint& lLimitHorizon, const agrint& iValue, bool bError)
{
    if (dimPd.IsEmpty()) 
        return bError;

    return lLimitHorizon >= iValue;
}

bool CSample::MethodP2(const TDimValue& dim1, const agrint& int1, const agrint& int2, bool bool1)
{
    // Implementation of MethodP2
    return true;
}

bool CSample::MethodP3(const TDimValue& dimVal, const agrint& intVal, const agrint& int2)
{
    // Implementation of MethodP3
    return false;
}

bool CSample::MethodP4() const
{
    // Implementation of MethodP4
    return cValue1 == cValue3;
}

bool CSample::MethodP5(const TDimValue& dim1, const agrint& int1, const agrint& int2)
{
    // Implementation of MethodP5
    return !dim1.IsEmpty() && int1 > int2;
}

bool CSample::MethodWithOverloads(const TDimValue& dim1)
{
    // Implementation of the first overload
    return !dim1.IsEmpty();
}

bool CSample::MethodWithOverloads(const TDimValue& dim1, const agrint& int1)
{
    // Implementation of the second overload
    return !dim1.IsEmpty() && int1 > 0;
}
