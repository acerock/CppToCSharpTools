#include "StdAfx.h"
#include "CSample.h"

CPartialSample::m_iIndex = -1;

CPartialSample::CPartialSample()
{
    m_value1 = 0;
 
    cValue1 = _T("ABC");
    cValue2 = _T("DEF");
    cValue3 = _T("GHI");
};

CPartialSample::~CPartialSample()
{
    // Cleanup code here
};

void CPartialSample::MethodOne(const CString& cParam1,
                   const bool &bParam2,
                   CString *pcParam3)
{
    // Implementation of MethodOne
}
