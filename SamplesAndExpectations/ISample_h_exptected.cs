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

    /* My struct */
    typedef struct
    {
        bool MyBoolField;
        agrint MyIntField;
    } MyStruct;

    public interface ISample
    {
        void MethodOne(const CString& cParam1, const bool &bParam2, CString *pcParam3);

        bool MethodTwo();

    }

    public static class ISampleExtensions
    {
        public static ISample GetInstance(this ISample instance)
        {
            return new CSample();
        }

    }
}
