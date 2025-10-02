public interface ISample
{
    void MethodOne(CString cParam1,
                   bool bParam2,
                   out CString pcParam3);

	bool MethodTwo();
}

public static class ISampleExtensions
{
    public static ISample GetInstance(this ISample sample)
    {
        CSample* pSample = new CSample();
        return pSample;
    }
}
