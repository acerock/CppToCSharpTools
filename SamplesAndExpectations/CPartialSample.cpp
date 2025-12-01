/* Top comment for CPartialSample file
 * We expect this comment on the top of the CPartialSample.cs file before the using statements.  
 */
#include "StdAfx.h"
#include "CPartialSample.h"

CPartialSample::m_iIndex = -1;

struct LocalStruct
{
protected:
    agrint m_iCounter;

public:
    TDimValue dimValue;

    LocalStruct(const TDimValue& firstDimValue)
    {
        dimValue = firstDimValue;
    }
};

#region Constructors

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

#endregion

#region Methods

StructOne* CPartialSample::MethodOne(const CString& cParam1,
                   const bool &bParam2,
                   CString *pcParam3)
{
    try
    {
        // Implementation of MethodOne
        return null;
    }
    catch (...)
    {
        return null;
    }
    return null;
}

#pragma endregion