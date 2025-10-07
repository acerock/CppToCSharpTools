using Agresso.Interface.CoreServices;
using Agresso.Types;
using BatchNet.Compatibility.Types;
using BatchNet.Fundamentals.Compatibility;
using U4.BatchNet.Common.Compatibility;
using static BatchNet.Compatibility.Level1;
using static BatchNet.Compatibility.BatchApi;

namespace Generated_CSample
{

    struct StructOne
    {
    agrint lTestType;
    #pragma region Just a h-file pragma test
    // att-id memeber comment
    TAttId attId;
    TDimValue dimVal;
    #pragma endregion // Comment test
    };

    internal class CSomeClass
    {
        private StructOne memberOne;
        private int memberTwo;

        public CSomeClass()
        {
            memberTwo = 33;
            memberOne.lTestType = 0;
        }

        public int GetMemberTwo()
        {
            /* Sample method body */
            return memberTwo;
        }

    }

    // Comment for class
    internal class CSample
    {
        private agrint m_value1;
        private CString cValue1;
        private CString cValue2;
        private CString cValue3;
            // Static member
        private static agrint m_iIndex = -1;
        private int m_iAnotherPrivateInteger;

        private CString PrivateMemberWithBodyInHfile(const TAttId &att_id)
        {
            if (cValue1.IsEmpty()) return _T("");
            return cValue1;
        }

            // Method with body in header file
        public bool MethodTwo()
        {
            return cValue1 == cValue2;
        }

        private int MethodPrivInl1(const TDimValue& dim1)
        {
            if (dim1.IsEmpty()) 
                return 0;

            return 42;
        }

        private void MethodPrivInl2(const TDimValue& dimPd, const agrint& lLimitHorizon, const agrint& iValue = 0, bool bError = false)
        {
            if (dimPd.IsEmpty()) 
                return bError;
            return lLimitHorizon >= iValue;
        }

        private int InlineMethodWithOverload(const TDimValue& dim1)
        {
            if (dim1.IsEmpty()) 
                return -1;
            return 100;
        }

        private int InlineMethodWithOverload(const TDimValue& dim1, bool bFlag, const CString& cPar = _T("xyz"))
        {
            if (dim1.IsEmpty() || cPar == _T("xyz") || !bFlag)
                return -2;
            return 200;
        }

        private int InlineMethodWithOverload(const TDimValue& dim1, bool bFlag, int i = 3)
        {
            if (dim1.IsEmpty()) 
                return -2;
            return 200;
        }

        public CSample()
        {
            m_value1 = 0;

            cValue1 = _T("ABC");
            cValue2 = _T("DEF");
            cValue3 = _T("GHI");
        }

        public ~CSample()
        {
            // Cleanup code here
        }

        public void MethodOne(const CString& cParam1, const bool &bParam2, CString *pcParam3)
        {
            // Implementation of MethodOne
        }

            // Comment from .h
        // Comment from .cpp
        private bool MethodP1(const TDimValue& dimPd, const agrint& lLimitHorizon, const agrint& iValue = 0, bool bError = false)
        {
            if (dimPd.IsEmpty()) 
                return bError;

            return lLimitHorizon >= iValue;
        }

        private bool MethodP2(const TDimValue& dim1, const agrint& int1, const agrint& int2 = 0, bool bool1 = false)
        {
            // Implementation of MethodP2
            return true;
        }

        private bool MethodP3(const TDimValue& dimVal, const agrint& intVal, const agrint& int2)
        {
            // Implementation of MethodP3
            return false;
        }

        private bool MethodP4()
        {
            // Implementation of MethodP4
            return cValue1 == cValue3;
        }

        private bool MethodP5(const TDimValue& dim1, const agrint& int1, const agrint& int2 = 0)
        {
            // Implementation of MethodP5
            return !dim1.IsEmpty() && int1 > int2;
        }

        private bool MethodWithOverloads(const TDimValue& dim1)
        {
            // Implementation of the first overload
            return !dim1.IsEmpty();
        }

        private bool MethodWithOverloads(const TDimValue& dim1, const agrint& int1)
        {
            // Implementation of the second overload
            return !dim1.IsEmpty() && int1 > 0;
        }

    }
}
