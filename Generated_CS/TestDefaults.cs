using Agresso.Interface.CoreServices;
using Agresso.Types;
using BatchNet.Compatibility.Types;
using BatchNet.Fundamentals.Compatibility;
using U4.BatchNet.Common.Compatibility;
using static BatchNet.Compatibility.Level1;
using static BatchNet.Compatibility.BatchApi;

namespace Generated_TestDefaults
{
    internal class TestDefaults
    {
        public void Method1(int param1 = 0, bool param2 = false);

        public void Method2(const char* str = "", int* ptr = nullptr);

        public void Method3(CString name = "DefaultName", agrint value = 42);

    }
}
