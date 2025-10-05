using Agresso.Types;
using BatchNet.Compatibility.Types;
using U4.BatchNet.Common.Compatibility;

namespace Generated_ISample
{
    public interface ISample
    {
        void MethodOne(const CString& cParam1, const bool &bParam2, CString *pcParam3);

        bool MethodTwo();

    }

    public static class ISampleExtensions
    {
        public static ISample* GetInstance(this ISample instance)
        {
            // TODO: Implementation not found
            throw new NotImplementedException();
        }

    }
}
