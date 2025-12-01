/* Top comment for CPartialSampleMethods file
 * We expect this comment on the top of the CPartialSampleMethods.cs file before the using statements.  
 */
#include "StdAfx.h"
#include "CPartialSample.h"
   
// This is a method comments and is associated to MethodOneInPartial
void CPartialSample::MethodOneInPartial(const CString& cParam1,
                   const bool &bParam2,
                   CString *pcParam3)
{
    struct DistrStruct
    {
    public:
        TAttId attId;
        TDimValue dimVal;
        CString cDimFlag;
        TAttId attDistrId;
        TDimValue dimDistr;
        TDimValue dimOrig;
        DistrStruct(){;};
    };

    DistrStruct as[7];

    TDimValue dimDistrAccount;
    // Implementation of MethodOneInPartial
}

void CPartialSample::MethodTwoInPartial(const CString& cParam1,
                   const bool &bParam2,
                   CString *pcParam3)
{
    // Implementation of MethodTwoInPartial
}

// Demo of non-class methods in partial implementation
bool LocalFunction2(const agrint& valueIn /* value in */)
{
    return valueIn > 0 && valueIn < 100;
}

/*
**  Find the appropriate value (dValue) for the currenct transaction.
*/
agrint CPartialSample::GetRate (
                 CAgrMT *pmtTrans, /*IN/OUT: Memory table with open cursor pointing to specific row containing relevant resource/position/date information to retrieve the rate */
                 double &dValue, /*OUT: Return value (rate) */
                 const TDimValue &dimValueId, /*IN: Value reference to retrieve value for */
                 CString &cTransDateFrom, /*IN/OUT: Date interval to retrieve rate for */
                 CString &cTransDateTo,   /*IN/OUT: ---''--- */
                 const CString &cDateLimit, /*IN: Set = "N" to avoid any split, or set according to PDRULE if you want to split transaction if rate changes during date-interval */
                 const double &dPostFlag) /*IN: Set = 1 if you want to look for rate according to position of transaction (if existing, 1.pri). Set = 0 if you want to ignore position when retrieving rate */
{
    TDimValue dimValueRate(_T(""));
    return GetRate (pmtTrans, dValue, dimValueId, dimValueRate, cTransDateFrom, cTransDateTo, cDateLimit, dPostFlag); 
}
