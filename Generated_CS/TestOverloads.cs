using Agresso.Interface.CoreServices;
using Agresso.Types;
using BatchNet.Compatibility.Types;
using BatchNet.Fundamentals.Compatibility;
using U4.BatchNet.Common.Compatibility;
using static BatchNet.Compatibility.Level1;
using static BatchNet.Compatibility.BatchApi;

namespace Generated_TestOverloads
{
    internal class TestOverloads
    {
        public void TestMethod(int param1, string param2);

        public void TestMethod(int param1)
        {
        // Implementation for single int parameter
        }

        public void TestMethod(float param1, bool param2)
        {
        // Implementation for float + bool parameters
        }

    }
}
