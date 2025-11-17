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

internal static class CStaticClass
{
    public static CString[] ColFrom = { _T("from_1"), _T("from_2"), _T("from_3"), _T("from_4") };
    public static CString[] ColTo = { _T("to_1"), _T("to_2"), _T("to_3"), _T("to_4") };
}

