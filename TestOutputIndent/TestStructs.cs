using Agresso.Types;
using BatchNet.Compatibility.Types;
using U4.BatchNet.Common.Compatibility;

namespace Generated_TestStructs
{
    // Test structs for parsing
    /* My struct */
    typedef struct
    {
        bool MyBoolField;
        agrint MyIntField;
    } MyStruct;


    struct SimpleStruct
    {
        bool SimpleBoolField;
        int SimpleIntField;
    };


    // This comment is for the other struct
    typedef struct MyTag
    {
        // This struct has a comment copied as is
        bool someBool;
        agrint intValue;
    } MyOtherStruct;

}
