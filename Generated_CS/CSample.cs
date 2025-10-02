using Agresso.Interface.CoreServices;
using Agresso.Types;
using BatchNet.Compatibility.Types;
using BatchNet.Fundamentals.Compatibility;
using U4.BatchNet.Common.Compatibility;
using static BatchNet.Compatibility.Level1;
using static BatchNet.Compatibility.BatchApi;

namespace Generated_CSample
{
    internal class StructOne
    {
        public agrint lTestType;
        public TAttId attId;
        public TDimValue dimVal;

    }

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

    internal class CSample
    {
        private agrint m_value1;
        private CString cValue1;
        private CString cValue2;
        private CString cValue3;
        private static agrint m_iIndex = -1;
        public int m_iAnotherPrivateInteger;

        public bool MethodTwo()
        {
            return cValue1 == cValue2;
        }

        public int MethodPrivInl1(const TDimValue& dim1)
        {
            if (dim1.IsEmpty()) 
                        return 0;
            
                    return 42;
        }

        public void MethodPrivInl2(const TDimValue& dimPd, const agrint& lLimitHorizon, const agrint& iValue = 0, bool bError = false)
        {
            if (dimPd.IsEmpty()) 
                        return bError;
                    return lLimitHorizon >= iValue;
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

        public bool MethodP1(const TDimValue& dimPd, const agrint& lLimitHorizon, const agrint& iValue, bool bError)
        {
        if (dimPd.IsEmpty()) 
                return bError;
        
            return lLimitHorizon >= iValue;
        }

        public bool MethodP2(const TDimValue& dim1, const agrint& int1, const agrint& int2 = 0, bool bool1 = false)
        {
        // Implementation of MethodP2
            return true;
        }

        public bool MethodP3(const TDimValue& dimVal, const agrint& intVal, const agrint& int2)
        {
        // Implementation of MethodP3
            return false;
        }

        public bool MethodP4()
        {
        // Implementation of MethodP4
            return cValue1 == cValue3;
        }

        public bool MethodP5(const TDimValue& dim1, const agrint& int1, const agrint& int2 = 0)
        {
        // Implementation of MethodP5
            return !dim1.IsEmpty() && int1 > int2;
        }

    }
}
