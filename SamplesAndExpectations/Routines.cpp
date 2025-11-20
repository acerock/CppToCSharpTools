/*

--- DESCRIPTION -------------------
        Some comments.

        Very useful descripition.
*/

#include "StdAfx.h"
#include "Routines.h"

bool SomeOtherStuff(const agrint& iTest, const CString& cMsg);

struct
{
    TAttId    attId;
    TDimValue dimAttName;
    CString   cDimId;
    agrint    lFlag;
    agrint    lPosition;
    agrint    lDimNo;
}   RoutineStruct;

void AGRRoutine(const TAttId &attId, bool bDoWorkd)
{
    agrint lMember1 = 0;

    TDimValue dimClient;
    TAttId    attParam;

    CString cTest = _T("MyRoutine");

    bool ok = SomeOtherStuff(3, _T("Hello"));

    /* Typical stuff goes here */
}

bool SomeOtherStuff(const agrint& iTest, const CString& cMsg)
{
    // Some other stuff happens here
    return TRUE;
}
