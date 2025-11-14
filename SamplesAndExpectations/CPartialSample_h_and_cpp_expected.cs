/* Top comment for CPartialSample file
 * We expect this comment on the top of the CPartialSample.cs file before the using statements.  
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

namespace U4.BatchNet.Sample.Compatibility
{

    // This should go into CPartialSample.cs
    struct StructOne
    {
        agrint lTestType;
        // att-id memeber comment
        TAttId attId;
        TDimValue dimVal;
    };

    // Comment for class
    internal partial class CPartialSample
    {
        private agrint m_value1;
            // Static member
        private static agrint m_iIndex = -1;

        // Methods for main file (inline + same-named source)
        private CString PrivateMemberWithBodyInHfile(const TAttId &att_id)
        {
            if (cValue1.IsEmpty()) return _T("");
            return cValue1;
        }

        private int MethodPrivInl1(const TDimValue& dim1)
        {
            if (dim1.IsEmpty()) 
                return 0;

            return 42;
        }

        public void CPartialSample()
        {
            m_value1 = 0;

            cValue1 = _T("ABC");
            cValue2 = _T("DEF");
            cValue3 = _T("GHI");
        }

        public void ~CPartialSample()
        {
            // Cleanup code here
        }

        public void MethodOne(const CString& cParam1, const bool &bParam2, CString *pcParam3)
        {
            // Implementation of MethodOne
        }

    }
}
