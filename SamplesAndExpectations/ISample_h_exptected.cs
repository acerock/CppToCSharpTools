using Agresso.Types;
using Agresso.Interface.CoreServices;
using BatchNet;
using BatchNet.Compatibility;
using BatchNet.Fundamentals.Compatibility;
using U4.BatchNet.ServerLib.Compatibility;
using static BatchNet.Compatibility.Level1;
using static BatchNet.Compatibility.Level2;
using static BatchNet.Compatibility.BatchApi;

namespace U4.BatchNet.Sample.Compatibility;

public static class SampleDefines
{
    // Some define
    const int IN_INTERFACE_DEF01 = 1;
    const int IN_INTERFACE_DEF02 = 2; // Another define
}


/* My struct */
public class MyStruct
{
    internal bool MyBoolField;
    internal agrint MyIntField;
} MyStruct;

[Create(typeof(CSample))]
public interface ISample
{
    void MethodOne(const CString& cParam1, const bool &bParam2, CString* pcParam3);

    bool MethodTwo();
}
