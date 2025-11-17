using System;
using System.IO;
using System.Linq;
using CppToCsConverter.Core.Core;
using CppToCsConverter.Core.Parsers;
using CppToCsConverter.Core.Models;
using Xunit;

namespace CppToCsConverter.Tests
{
    public class StructGenerationTests
    {
        private readonly CppToCsStructuralConverter _converter;

        public StructGenerationTests()
        {
            _converter = new CppToCsStructuralConverter();
        }

        [Fact]
        public void Generate_SimpleStruct_TransformsToInternalClass()
        {
            // Arrange
            var content = @"
struct MyStruct
{
    bool MyBoolField;
    agrint MyIntField;
};";

            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            var tempFile = Path.Combine(tempDir, "MyStruct.h");
            var outputDir = Path.Combine(tempDir, "output");
            
            File.WriteAllText(tempFile, content);

            try
            {
                // Act
                _converter.ConvertDirectory(tempDir, outputDir);

                // Assert
                var outputFile = Path.Combine(outputDir, "MyStruct.cs");
                Assert.True(File.Exists(outputFile), $"Output file not found: {outputFile}");
                
                var result = File.ReadAllText(outputFile);
                
                // Should be internal class, not typedef struct
                Assert.Contains("internal class MyStruct", result);
                Assert.DoesNotContain("typedef struct", result);
                Assert.DoesNotContain("struct MyStruct", result);
                
                // Members should have internal modifier
                Assert.Contains("internal bool MyBoolField;", result);
                Assert.Contains("internal agrint MyIntField;", result);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void Generate_TypedefStruct_TransformsToInternalClass()
        {
            // Arrange
            var content = @"
/* My struct */
typedef struct
{
    bool MyBoolField;
    agrint MyIntField;
} MyStruct;";

            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            var tempFile = Path.Combine(tempDir, "MyStruct.h");
            var outputDir = Path.Combine(tempDir, "output");
            
            File.WriteAllText(tempFile, content);

            try
            {
                // Act
                _converter.ConvertDirectory(tempDir, outputDir);

                // Assert
                var outputFile = Path.Combine(outputDir, "MyStruct.cs");
                Assert.True(File.Exists(outputFile));
                
                var result = File.ReadAllText(outputFile);
                
                // Should preserve preceding comment
                Assert.Contains("/* My struct */", result);
                
                // Should be internal class, not typedef struct
                Assert.Contains("internal class MyStruct", result);
                Assert.DoesNotContain("typedef struct", result);
                
                // Members should have internal modifier
                Assert.Contains("internal bool MyBoolField;", result);
                Assert.Contains("internal agrint MyIntField;", result);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void Generate_TypedefStructTag_TransformsToInternalClass()
        {
            // Arrange
            var content = @"
typedef struct MyTag
{
    bool MyBoolField;
    agrint MyIntField;
} MyStruct;";

            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            var tempFile = Path.Combine(tempDir, "MyStruct.h");
            var outputDir = Path.Combine(tempDir, "output");
            
            File.WriteAllText(tempFile, content);

            try
            {
                // Act
                _converter.ConvertDirectory(tempDir, outputDir);

                // Assert
                var outputFile = Path.Combine(outputDir, "MyStruct.cs");
                Assert.True(File.Exists(outputFile));
                
                var result = File.ReadAllText(outputFile);
                
                // Should be internal class with MyStruct name (not MyTag)
                Assert.Contains("internal class MyStruct", result);
                Assert.DoesNotContain("typedef struct", result);
                Assert.DoesNotContain("MyTag", result);
                
                // Members should have internal modifier
                Assert.Contains("internal bool MyBoolField;", result);
                Assert.Contains("internal agrint MyIntField;", result);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void Generate_StructWithComments_PreservesComments()
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

            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            var tempFile = Path.Combine(tempDir, "MyStruct.h");
            var outputDir = Path.Combine(tempDir, "output");
            
            File.WriteAllText(tempFile, content);

            try
            {
                // Act
                _converter.ConvertDirectory(tempDir, outputDir);

                // Assert
                var outputFile = Path.Combine(outputDir, "MyStruct.cs");
                var result = File.ReadAllText(outputFile);
                
                // Should preserve all comments
                Assert.Contains("/* My struct */", result);
                Assert.Contains("// This is a bool field", result);
                Assert.Contains("// This is an int field", result);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void Generate_StructWithInterface_PlacedCorrectly()
        {
            // Arrange - Struct with interface in same file
            var content = @"
/* My struct */
typedef struct
{
    bool MyBoolField;
    agrint MyIntField;
} MyStruct;

/* The Interface */
class __declspec(dllexport) ISample
{
public:
    virtual ~ISample(){};
    static ISample* GetInstance();

    virtual void MethodOne(const CString& cParam1,
                           const bool &bParam2,
                           CString *pcParam3) = 0;

    virtual bool MethodTwo() = 0;
};";

            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            var tempFile = Path.Combine(tempDir, "ISample.h");
            var outputDir = Path.Combine(tempDir, "output");
            
            File.WriteAllText(tempFile, content);

            try
            {
                // Act
                _converter.ConvertDirectory(tempDir, outputDir);

                // Assert
                var outputFile = Path.Combine(outputDir, "ISample.cs");
                Assert.True(File.Exists(outputFile));
                
                var result = File.ReadAllText(outputFile);
                
                // Struct should be before interface
                Assert.Contains("internal class MyStruct", result);
                Assert.Contains("public interface ISample", result);
                
                var structIndex = result.IndexOf("internal class MyStruct");
                var interfaceIndex = result.IndexOf("public interface ISample");
                Assert.True(structIndex < interfaceIndex, "Struct should appear before interface");
                
                // Struct members should have internal modifier
                Assert.Contains("internal bool MyBoolField;", result);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void Generate_MultipleStructsInFile_AllTransformed()
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

            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            var tempFile = Path.Combine(tempDir, "MyStructs.h");
            var outputDir = Path.Combine(tempDir, "output");
            
            File.WriteAllText(tempFile, content);

            try
            {
                // Act
                _converter.ConvertDirectory(tempDir, outputDir);

                // Assert
                var outputFile = Path.Combine(outputDir, "MyStructs.cs");
                Assert.True(File.Exists(outputFile));
                
                var result = File.ReadAllText(outputFile);
                
                // Both structs should be internal classes
                Assert.Contains("internal class MyStruct", result);
                Assert.Contains("internal class MyOtherStruct", result);
                
                // Comments should be preserved
                Assert.Contains("/* My struct */", result);
                Assert.Contains("// This comment is for the other struct", result);
                Assert.Contains("// This struct has a comment copied as is", result);
                
                // All members should have internal modifier
                Assert.Contains("internal bool MyBoolField;", result);
                Assert.Contains("internal agrint MyIntField;", result);
                Assert.Contains("internal bool someBool;", result);
                Assert.Contains("internal agrint intValue;", result);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void Generate_StructWithPointerMembers_TransformsCorrectly()
        {
            // Arrange
            var content = @"
struct MyStruct
{
    CString* pStringField;
    const CString* pConstStringField;
};";

            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            var tempFile = Path.Combine(tempDir, "MyStruct.h");
            var outputDir = Path.Combine(tempDir, "output");
            
            File.WriteAllText(tempFile, content);

            try
            {
                // Act
                _converter.ConvertDirectory(tempDir, outputDir);

                // Assert
                var outputFile = Path.Combine(outputDir, "MyStruct.cs");
                var result = File.ReadAllText(outputFile);
                
                // Should preserve pointer syntax
                Assert.Contains("internal CString* pStringField;", result);
                Assert.Contains("internal const CString* pConstStringField;", result);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }
    }
}
