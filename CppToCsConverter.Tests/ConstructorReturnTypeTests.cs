using System.IO;
using System.Linq;
using Xunit;
using CppToCsConverter.Core;

namespace CppToCsConverter.Tests
{
    /// <summary>
    /// Tests to ensure constructors are generated without return types
    /// Constructors should NEVER have "void" or any other return type
    /// </summary>
    public class ConstructorReturnTypeTests
    {
        [Fact]
        public void Constructor_InPartialClass_ShouldNotHaveReturnType()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);

            try
            {
                // Create header file with constructor declaration
                var headerFile = Path.Combine(tempDir, "TestClass.h");
                var headerContent = @"
class TestClass
{
public:
    TestClass();
    void MethodOne();
};";
                File.WriteAllText(headerFile, headerContent);

                // Create source file with constructor implementation
                var sourceFile = Path.Combine(tempDir, "TestClass.cpp");
                var sourceContent = @"
#include ""TestClass.h""

TestClass::TestClass()
{
    // Constructor implementation
}

void TestClass::MethodOne()
{
    // Method implementation
}";
                File.WriteAllText(sourceFile, sourceContent);

                var outputDir = Path.Combine(tempDir, "output");
                Directory.CreateDirectory(outputDir);

                // Act
                var converter = new CppToCsConverterApi();
                converter.ConvertDirectory(tempDir, outputDir);

                // Assert
                var outputFile = Path.Combine(outputDir, "TestClass.cs");
                Assert.True(File.Exists(outputFile));

                var generatedContent = File.ReadAllText(outputFile);
                
                // Constructor should NOT have "void" keyword
                Assert.DoesNotContain("public void TestClass()", generatedContent);
                Assert.DoesNotContain("void TestClass()", generatedContent);
                
                // Constructor should have correct format
                Assert.Contains("public TestClass()", generatedContent);
                
                // Regular method SHOULD have void return type
                Assert.Contains("public void MethodOne()", generatedContent);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void Constructor_WithParameters_ShouldNotHaveReturnType()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);

            try
            {
                var headerFile = Path.Combine(tempDir, "Sample.h");
                var headerContent = @"
class Sample
{
public:
    Sample(int value);
};";
                File.WriteAllText(headerFile, headerContent);

                var sourceFile = Path.Combine(tempDir, "Sample.cpp");
                var sourceContent = @"
#include ""Sample.h""

Sample::Sample(int value)
{
    // Constructor with parameter
}";
                File.WriteAllText(sourceFile, sourceContent);

                var outputDir = Path.Combine(tempDir, "output");
                Directory.CreateDirectory(outputDir);

                // Act
                var converter = new CppToCsConverterApi();
                converter.ConvertDirectory(tempDir, outputDir);

                // Assert
                var outputFile = Path.Combine(outputDir, "Sample.cs");
                var generatedContent = File.ReadAllText(outputFile);
                
                // Constructor should NOT have void
                Assert.DoesNotContain("void Sample(", generatedContent);
                Assert.Contains("public Sample(int value)", generatedContent);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void Constructor_InLocalStruct_ShouldNotHaveReturnType()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);

            try
            {
                var headerFile = Path.Combine(tempDir, "MyClass.h");
                var headerContent = @"
class MyClass
{
public:
    void Process();
};";
                File.WriteAllText(headerFile, headerContent);

                var sourceFile = Path.Combine(tempDir, "MyClass.cpp");
                var sourceContent = @"
#include ""MyClass.h""

struct LocalStruct
{
    int m_counter;

    LocalStruct(int value)
    {
        m_counter = value;
    }
};

void MyClass::Process()
{
    LocalStruct local(5);
}";
                File.WriteAllText(sourceFile, sourceContent);

                var outputDir = Path.Combine(tempDir, "output");
                Directory.CreateDirectory(outputDir);

                // Act
                var converter = new CppToCsConverterApi();
                converter.ConvertDirectory(tempDir, outputDir);

                // Assert
                var outputFile = Path.Combine(outputDir, "MyClass.cs");
                var generatedContent = File.ReadAllText(outputFile);
                
                // LocalStruct constructor should NOT have void
                Assert.DoesNotContain("void LocalStruct(", generatedContent);
                Assert.Contains("public LocalStruct(int value)", generatedContent);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void Destructor_ShouldNotBeGenerated()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);

            try
            {
                var headerFile = Path.Combine(tempDir, "TestClass.h");
                var headerContent = @"
class TestClass
{
public:
    TestClass();
    ~TestClass();
};";
                File.WriteAllText(headerFile, headerContent);

                var sourceFile = Path.Combine(tempDir, "TestClass.cpp");
                var sourceContent = @"
#include ""TestClass.h""

TestClass::TestClass()
{
}

TestClass::~TestClass()
{
    // Cleanup
}";
                File.WriteAllText(sourceFile, sourceContent);

                var outputDir = Path.Combine(tempDir, "output");
                Directory.CreateDirectory(outputDir);

                // Act
                var converter = new CppToCsConverterApi();
                converter.ConvertDirectory(tempDir, outputDir);

                // Assert
                var outputFile = Path.Combine(outputDir, "TestClass.cs");
                var generatedContent = File.ReadAllText(outputFile);
                
                // Destructor should be generated but without "void" keyword
                Assert.Contains("~TestClass()", generatedContent);
                Assert.DoesNotContain("void ~TestClass()", generatedContent);
                
                // Constructor should also not have void
                Assert.Contains("public TestClass()", generatedContent);
                Assert.DoesNotContain("void TestClass()", generatedContent);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void Constructor_InlineInHeader_ShouldNotHaveReturnType()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);

            try
            {
                var headerFile = Path.Combine(tempDir, "InlineTest.h");
                var headerContent = @"
class InlineTest
{
public:
    InlineTest() { /* inline constructor */ }
    void Method() { /* inline method */ }
};";
                File.WriteAllText(headerFile, headerContent);

                var outputDir = Path.Combine(tempDir, "output");
                Directory.CreateDirectory(outputDir);

                // Act
                var converter = new CppToCsConverterApi();
                converter.ConvertDirectory(tempDir, outputDir);

                // Assert
                var outputFile = Path.Combine(outputDir, "InlineTest.cs");
                var generatedContent = File.ReadAllText(outputFile);
                
                // Inline constructor should NOT have void
                Assert.DoesNotContain("void InlineTest()", generatedContent);
                Assert.Contains("public InlineTest()", generatedContent);
                
                // Inline method SHOULD have void
                Assert.Contains("void Method()", generatedContent);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }
    }
}
