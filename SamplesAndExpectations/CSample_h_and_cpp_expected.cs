/* Top comment for CSample file
 * We expect this comment on the top of the CSample.cs file before the using statements.  
 */
using Agresso.Types;
using Agresso.Interface.CoreServices;
using BatchNet;
using BatchNet.Compatibility;
using BatchNet.Fundamentals.Compatibility;
using U4.BatchNet.ServerLib.Compatibility;
using static BatchNet.Compatibility.Level1;
using static BatchNet.Compatibility.Level2;
using static BatchNet.Compatibility.BatchApi;

namespace U4.BatchNet.Sample.Compatibility;

internal class StructOne
{
    protected agrint lTestType;

    //#region Just a h-file pragma test

    // att-id member comment
    public TAttId attId;
    public TDimValue dimVal;

    //#endregion  // Comment test

    public StructOne(const TAttid& inAttId, const TDimValue &inDimVal, agrint lInTestType = 0)
    {
        lTestType = lInTestType;
        attid = inAttId;
        dimVal = inDimVal;
    }
}

internal class CSomeClass
{
    private StructOne memberOne;
    private int memberTwo;
    // Array of ints
    private agrint[] m_aIntArr1 = new agrint[ARR_SIZE]; // Comment about ARR_SIZE

    public CSomeClass()
    {
        memberTwo = 33;
        memberOne.lTestType = 0;
    }

    public int GetMemberTwo()
    {
        struct tm time;
        /* Sample method body */
        try
        {
            // Something
        }
        catch (...)
        {
            
        }

        return memberTwo;
    }
}

// Comment for class
internal class CSample : ISample
{
    // Top defines
    internal const int MY_DEFINE = 1;
    internal const int MY_DEFINE2 = 2;
    // Comment for define 3
    internal const int MY_DEFINE3 = 3;
    // Some more defines
    private const int MY_DEFINE4 = 4;
    private const int MY_DEFINE5 = 5;
    private const int CPP_DEFINE4 = 40;

    private agrint m_value1;

    private CString cValue1;
    private CString cValue2;
    private CString cValue3;
    private CAgrMT* m_pmtReport; //Res/Rate-Reporting (To do: Not touch?)

    // Static member

    private static agrint m_iIndex = -1;

    // Comment
    private const CString s_cStructFlatType = "N"; // More comment

    public const agrint gs_lStructLevelGLDimensionMin = 1;

    public CSample()
    {
        m_value1 = 0;

        cValue1 = "ABC";
        cValue2 = "DEF";
        cValue3 = "GHI";
    }

    ~CSample()
    {
        // Cleanup code here
    }

    // Demo of non-class methods
    bool LocalFunction(const agrint& valueIn /* value in */)
    {
        return valueIn > 0 && valueIn < 100;
    }

    public void MethodOne(CString cParam1,
                bool bParam2,
                out CString pcParam3)
    {
        // Implementation of MethodOne
    }

    // Comment from .h
    // Comment from .cpp
    private bool MethodP1(TDimValue dimPd, agrint lLimitHorizon, agrint iValue, bool bError)
    {
        if (dimPd.IsEmpty())
            return bError;

        return lLimitHorizon >= iValue;
    }

    private bool MethodP2(TDimValue dim1, agrint int1, agrint int2, bool bool1)
    {
        // Implementation of MethodP2
        return true;
    }

    private bool MethodP3(TDimValue dimVal, agrint intVal, agrint int2)
    {
        // Implementation of MethodP3
        return false;
    }

    private bool MethodP4()
    {
        // Implementation of MethodP4
        return cValue1 == cValue3;
    }

    // Method with body in header file
    public bool MethodTwo() { return cValue1 == cValue2; }

    private CString PrivateMemberWithBodyInHfile(TAttId att_id)
    {
        if (cValue1.IsEmpty()) return "";

        return cValue1;
    }

    private int MethodPrivInl1(TDimValue dim1)
    {
        if (dim1.IsEmpty())
            return 0;

        return 42;
    }

    private bool MethodPrivInl2(TDimValue dimPd, agrint lLimitHorizon, agrint iValue = 0, bool bError = false)
    {
        if (dimPd.IsEmpty())
            return bError;

        return lLimitHorizon >= iValue;
    }

    bool MethodWithOverloads(const TDimValue& dim1)
    {
        // Implementation of the first overload
        return !dim1.IsEmpty();
    }

    int InlineMethodWithOverload(const TDimValue& dim1, bool bFlag, const CString& cPar = _T("xyz"))
    {
        if (dim1.IsEmpty() || cPar == _T("xyz") || !bFlag)
            return -2;
        return 200;
    }

    bool MethodWithOverloads(const TDimValue& dim1, const agrint& int1)
    {
        // Implementation of the second overload
        return !dim1.IsEmpty() && int1 > 0;
    }

    CSomeClass* InlineWithPointerReturn()
    {
        return new CSomeClass();
    }

    private void TrickyToMatch(
        /* IN*/const CString& cResTab, 
        /* IN */ const bool& bGetAgeAndTaxNumberFromResTab, 
        /* OUT */ CAgrMT* pmtTable)
    {
        // Implementation of TrickyToMatch
        if (cResTab.IsEmpty() || pmtTable == NULL) {
            return;
        }

        if (bGetAgeAndTaxNumberFromResTab) {
            // Some logic here
        }

        return false;
    }
    
    private int m_iAnotherPrivateInteger;
}    


