/* Top comment for CPartialSampleMethods file
 * We expect this comment on the top of the CPartialSampleMethods.cs file before the using statements.  
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

internal partial class CPartialSample
{
    protected bool GetRelValue (const agrint &lIdRes,
                    const TAttId &attRelId, 
                    const TDimValue &dimPostId, 
                    const CString &cTransDateFrom, 
                    const CString &cTransDateTo,
                    TDimValue &dimRelValue, 
                    CString &cValidFrom,
                    CString &cValidTo)
    {
        bool bOnlyPosLevel = false;  // Don't switch off!! I.e: do stuff
        // We have comments there (yes)
        return GetRelValue (lIdRes,attRelId,dimPostId,cTransDateFrom,cTransDateTo,false,false,false,dimRelValue, cValidFrom, cValidTo, bOnlyPosLevel);
    }

    protected bool GetRelValue (const agrint &lIdRes,
                    const TAttId &attRelId, 
                    const TDimValue &dimPostId, 
                    const CString &cTransDateFrom, 
                    const CString &cTransDateTo,
                    bool bAnalysis,   // Comment 1,
                    bool bRate,       // Comment 2 (with more)
                    bool bReport,     // Comment 3
                    TDimValue &dimRelValue, 
                    CString &cValidFrom,
                    CString &cValidTo,
                    bool &bOnlyPosLevel)
    {
        return GetRelValue (lIdRes,attRelId,dimPostId,cTransDateFrom,cTransDateTo,bAnalysis,bRate,bReport,false,dimRelValue, cValidFrom, cValidTo,bOnlyPosLevel);
    }

    protected bool GetRelValue (const agrint &lIdRes,
                    const TAttId &attRelId, 
                    const TDimValue &dimPostId, 
                    const CString &cTransDateFrom, 
                    const CString &cTransDateTo,
                    bool bAnalysis,   // Comment 1,
                    bool bRate,       // Commen 2 (with more)
                    bool bReport,     // Comment 3
                    bool bUseFirstStartingRel,
                    TDimValue &dimRelValue, 
                    CString &cValidFrom,
                    CString &cValidTo,
                    bool &bOnlyPosLevel){
        // Some very long logic here
        return true;
    }
}

