/* My struct */
typedef struct
{
    bool MyBoolField;
    agrint MyIntField;
} MyStruct;

/* The Interface */
public interface ISample
{
    void MethodOne(CString cParam1,
                   bool bParam2,
                   out CString pcParam3);

    bool MethodTwo();
}

internal static class ISampleExtensions
{
    public static ISample GetInstance(this ISample sample)
    {
        CSample* pSample = new CSample();
        return pSample;
    }
}
