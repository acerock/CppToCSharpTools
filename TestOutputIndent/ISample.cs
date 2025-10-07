using Agresso.Types;
using BatchNet.Compatibility.Types;
using U4.BatchNet.Common.Compatibility;

namespace Generated_ISample
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
