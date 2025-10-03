using Agresso.Interface.CoreServices;
using Agresso.Types;
using BatchNet.Compatibility.Types;
using BatchNet.Fundamentals.Compatibility;
using U4.BatchNet.Common.Compatibility;
using static BatchNet.Compatibility.Level1;
using static BatchNet.Compatibility.BatchApi;

namespace Generated_TestArrays
{
    internal class TestArraysClass
    {
        public static int RegularStaticMember = 42;
        public static CString[] StaticStringArrays = { _T("test1"), _T("test2") };
        public static int[] StaticIntArray = { 10, 20, 30 };
        public int instanceMember;
        public CString[] instanceArray;

        public void InstanceMethod();

        public static int StaticMethod();

    }

    internal static class PureStaticClass
    {
        public static CString[] OnlyStaticArrays = { _T("pure1"), _T("pure2") };
        public static int OnlyStaticValue = 99;

    }
}
