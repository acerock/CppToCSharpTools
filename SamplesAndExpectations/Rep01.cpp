/*

--- DESCRIPTION -------------------
        Report 01.

        Very useful descripition.
*/

#include "StdAfx.h"
#include "ISample.h"

MAIN(rep01)
{
    agrint lMember1 = 0;

    TDimValue dimClient;
    TAttId    attParam;

    CString cTest = _T("Test");
    CString cRelTable;
    CString cMethodOneResult;

    // Local struct comes here
    typedef struct attinfotag
    {
        TAttId    attId;
        TDimValue dimAttName;
        CString   cDimId;
        agrint    lFlag;
        agrint    lPosition;
        agrint    lDimNo;
    }   attinfo;

    attinfo asAggDim[10];  /* Some comment   */

    AGRGetParam (_T("client"),       dimClient);

    AGRGetTableName ( cRelTable );

    if (AGRGetSysconf (_T("MY_SYS_SETUP"), attParam) == NOTOK)
        AGRRepStop (_T("No attribute defined for INT_CALC_ATT."));

    // This should work as GetInstance() will become an extension method for the interface
    ISample pSample = ISample->GetInstance();
    pSample->MethodOne(_T("TEST 1"), TRUE, cMethodOneResult);

    AGRCallReport (_T("Rep01"));

    return 0;
}
