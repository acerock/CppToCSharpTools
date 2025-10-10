#pragma once

#include "ISample.h"

// This should go into CPartialSample.cs
struct StructOne
{
    agrint lTestType;

    // att-id memeber comment
    TAttId attId;
    TDimValue dimVal;
};

// Comment for class
class CPartialSample
{
private:
    agrint m_value1;

    // Static member
    static agrint m_iIndex;

    CString PrivateMemberWithBodyInHfile(const TAttId &att_id)
    {
        if (cValue1.IsEmpty()) return _T("");

        return cValue1;
    }

public:
    CPartialSample();
    ~CPartialSample();

    void MethodOne(const CString& cParam1,
                   const bool &bParam2,
                   CString *pcParam3);
    void MethodOneInPartial(const CString& cParam1,
                   const bool &bParam2,
                   CString *pcParam3);
                   
	agrint GetRate(CAgrMT* pmtTrans, double& dValue, const TDimValue& dimValueId, CString& cTransDateFrom, CString& cTransDateTo, const CString& cDateLimit, const double& dPostFlag);

    void MethodTwoInPartial(const CString& cParam1,
                   const bool &bParam2,
                   CString *pcParam3);

private:
    int MethodPrivInl1(const TDimValue& dim1)
    {
        if (dim1.IsEmpty()) 
            return 0;
        
        return 42;
    }
};
