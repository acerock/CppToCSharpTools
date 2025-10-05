using Agresso.Interface.CoreServices;
using Agresso.Types;
using BatchNet.Compatibility.Types;
using BatchNet.Fundamentals.Compatibility;
using U4.BatchNet.Common.Compatibility;
using static BatchNet.Compatibility.Level1;
using static BatchNet.Compatibility.BatchApi;

namespace Generated_TestAllBasicTypes
{
    internal class TestAllBasicTypes
    {
            // Basic signed types
        private char charVar;
        private short shortVar;
        private int intVar;
        private long longVar;
            // Floating point types
        private float floatVar;
        private double doubleVar;
            // Size and special types
        private size_t sizeVar;
            // User-defined types
        private agrint agrintVar;
        private CString cstringVar;
            // Windows API types
        private DWORD dwordVar;
        private LPSTR lpstrVar;

        public void TestMethod(unsigned char param1, size_t param2 = 0, const unsigned long& param3 = 100, DWORD* param4 = nullptr);

    }
}
