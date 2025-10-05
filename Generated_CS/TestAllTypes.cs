using Agresso.Interface.CoreServices;
using Agresso.Types;
using BatchNet.Compatibility.Types;
using BatchNet.Fundamentals.Compatibility;
using U4.BatchNet.Common.Compatibility;
using static BatchNet.Compatibility.Level1;
using static BatchNet.Compatibility.BatchApi;

namespace Generated_TestAllTypes
{
    internal class TestAllTypes
    {
            // Basic C++ types
        private int basicInt;
        private bool basicBool;
            // User-defined types
        private agrint customInt;
        private CString customString;
            // Windows API types
        private DWORD winDWord;
        private LPSTR winLPStr;

        public void TestMethod(const CString& param1, DWORD param2 = 0, bool* param3 = nullptr, std::string param4 = "default");

        public LPSTR GetString();

        public agrint GetValue(const std::vector<int>& vec);

    }
}
