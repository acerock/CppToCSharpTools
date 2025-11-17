using System;
using System.IO;
using System.Linq;
using CppToCsConverter.Core.Parsers;
using CppToCsConverter.Core.Models;
using Xunit;

namespace CppToCsConverter.Tests
{
    public class StructParsingTests
    {
        private readonly CppHeaderParser _parser;

        public StructParsingTests()
        {
            _parser = new CppHeaderParser();
        }

        [Fact]
        public void Parse_SimpleStruct_ExtractsMembersCorrectly()
        {
            // Arrange
            var content = @"
struct MyStruct
{
    bool MyBoolField;
    agrint MyIntField;
};";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, content);

            try
            {
                // Act
                var structs = _parser.ParseStructsFromHeaderFile(tempFile);

                // Assert
                Assert.Single(structs);
                var myStruct = structs[0];
                Assert.Equal("MyStruct", myStruct.Name);
                Assert.Equal(StructType.Simple, myStruct.Type);
                Assert.Equal(2, myStruct.Members.Count);
                
                Assert.Equal("MyBoolField", myStruct.Members[0].Name);
                Assert.Equal("bool", myStruct.Members[0].Type);
                Assert.Equal(AccessSpecifier.Internal, myStruct.Members[0].AccessSpecifier);
                
                Assert.Equal("MyIntField", myStruct.Members[1].Name);
                Assert.Equal("agrint", myStruct.Members[1].Type);
                Assert.Equal(AccessSpecifier.Internal, myStruct.Members[1].AccessSpecifier);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void Parse_TypedefStruct_ExtractsMembersCorrectly()
        {
            // Arrange
            var content = @"
typedef struct
{
    bool MyBoolField;
    agrint MyIntField;
} MyStruct;";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, content);

            try
            {
                // Act
                var structs = _parser.ParseStructsFromHeaderFile(tempFile);

                // Assert
                Assert.Single(structs);
                var myStruct = structs[0];
                Assert.Equal("MyStruct", myStruct.Name);
                Assert.Equal(StructType.Typedef, myStruct.Type);
                Assert.Equal(2, myStruct.Members.Count);
                
                Assert.Equal("MyBoolField", myStruct.Members[0].Name);
                Assert.Equal("bool", myStruct.Members[0].Type);
                
                Assert.Equal("MyIntField", myStruct.Members[1].Name);
                Assert.Equal("agrint", myStruct.Members[1].Type);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void Parse_TypedefStructTag_ExtractsMembersCorrectly()
        {
            // Arrange
            var content = @"
typedef struct MyTag
{
    bool MyBoolField;
    agrint MyIntField;
} MyStruct;";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, content);

            try
            {
                // Act
                var structs = _parser.ParseStructsFromHeaderFile(tempFile);

                // Assert
                Assert.Single(structs);
                var myStruct = structs[0];
                Assert.Equal("MyStruct", myStruct.Name);
                Assert.Equal(StructType.TypedefTag, myStruct.Type);
                Assert.Equal(2, myStruct.Members.Count);
                
                Assert.Equal("MyBoolField", myStruct.Members[0].Name);
                Assert.Equal("bool", myStruct.Members[0].Type);
                
                Assert.Equal("MyIntField", myStruct.Members[1].Name);
                Assert.Equal("agrint", myStruct.Members[1].Type);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void Parse_StructWithPrecedingComments_PreservesComments()
        {
            // Arrange
            var content = @"
/* My struct */
typedef struct
{
    // This is a bool field
    bool MyBoolField;
    agrint MyIntField; // This is an int field
} MyStruct;";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, content);

            try
            {
                // Act
                var structs = _parser.ParseStructsFromHeaderFile(tempFile);

                // Assert
                Assert.Single(structs);
                var myStruct = structs[0];
                Assert.Equal("MyStruct", myStruct.Name);
                Assert.Single(myStruct.PrecedingComments);
                Assert.Contains("My struct", myStruct.PrecedingComments[0]);
                
                // Check member comments
                Assert.Equal(2, myStruct.Members.Count);
                Assert.Single(myStruct.Members[0].PrecedingComments);
                Assert.Contains("This is a bool field", myStruct.Members[0].PrecedingComments[0]);
                
                Assert.Contains("This is an int field", myStruct.Members[1].PostfixComment);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void Parse_StructWithPointerMembers_ParsesCorrectly()
        {
            // Arrange
            var content = @"
struct MyStruct
{
    CString* pStringField;
    const CString* pConstStringField;
    CAgrMT* pmtTable;
};";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, content);

            try
            {
                // Act
                var structs = _parser.ParseStructsFromHeaderFile(tempFile);

                // Assert
                Assert.Single(structs);
                var myStruct = structs[0];
                Assert.Equal(3, myStruct.Members.Count);
                
                Assert.Equal("pStringField", myStruct.Members[0].Name);
                Assert.Contains("CString", myStruct.Members[0].Type);
                Assert.Contains("*", myStruct.Members[0].Type);
                
                Assert.Equal("pConstStringField", myStruct.Members[1].Name);
                Assert.Contains("const", myStruct.Members[1].Type);
                Assert.Contains("CString", myStruct.Members[1].Type);
                
                Assert.Equal("pmtTable", myStruct.Members[2].Name);
                Assert.Contains("CAgrMT", myStruct.Members[2].Type);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void Parse_MultipleStructsInFile_ParsesAllCorrectly()
        {
            // Arrange
            var content = @"
/* My struct */
typedef struct
{
    bool MyBoolField;
    agrint MyIntField;
} MyStruct;

// This comment is for the other struct

typedef struct MyTag
{
    // This struct has a comment copied as is
    bool someBool;
    agrint intValue;
} MyOtherStruct;";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, content);

            try
            {
                // Act
                var structs = _parser.ParseStructsFromHeaderFile(tempFile);

                // Assert
                Assert.Equal(2, structs.Count);
                
                // First struct
                Assert.Equal("MyStruct", structs[0].Name);
                Assert.Equal(2, structs[0].Members.Count);
                Assert.Single(structs[0].PrecedingComments);
                
                // Second struct
                Assert.Equal("MyOtherStruct", structs[1].Name);
                Assert.Equal(2, structs[1].Members.Count);
                Assert.Single(structs[1].PrecedingComments);
                Assert.Contains("comment is for the other struct", structs[1].PrecedingComments[0]);
                
                // Check second struct member comment
                Assert.Single(structs[1].Members[0].PrecedingComments);
                Assert.Contains("This struct has a comment", structs[1].Members[0].PrecedingComments[0]);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void Parse_StructMembersWithVariousAccessModifiers_AllGetInternalAccess()
        {
            // Arrange - In C++ structs, members are public by default, but we want internal in C#
            var content = @"
struct MyStruct
{
    bool publicField;
private:
    agrint privateField;
protected:
    CString protectedField;
public:
    double publicField2;
};";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, content);

            try
            {
                // Act
                var structs = _parser.ParseStructsFromHeaderFile(tempFile);

                // Assert
                Assert.Single(structs);
                var myStruct = structs[0];
                
                // All struct members should have Internal access regardless of C++ access specifiers
                foreach (var member in myStruct.Members)
                {
                    Assert.Equal(AccessSpecifier.Internal, member.AccessSpecifier);
                }
            }
            finally
            {
                File.Delete(tempFile);
            }
        }
    }
}
