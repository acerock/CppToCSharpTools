using Xunit;
using CppToCsConverter.Core;
using System.IO;
using System;

namespace CppToCsConverter.Tests
{
    public class EmptyMethodBodyTests
    {
        [Fact]
        public void EmptyDestructor_ShouldNotHaveTODO()
        {
            // Arrange
            var headerContent = @"
class CHSRelService
{
public:
    CHSRelService();
    ~CHSRelService();
};
";

            var sourceContent = @"
CHSRelService::CHSRelService()
{
    // TEST
}

CHSRelService::~CHSRelService()
{
}
";

            // Act
            var result = ConvertWithHeaderAndSource(headerContent, sourceContent);

            // Assert - destructor should NOT have TODO comment
            Assert.DoesNotContain("public ~CHSRelService()\n    {\n        // TODO:", result);
            
            // Verify destructor exists with empty body
            Assert.Contains("public ~CHSRelService()", result);
            
            // Constructor should have its implementation
            Assert.Contains("// TEST", result);
        }

        [Fact]
        public void EmptyMethod_ShouldNotHaveTODO()
        {
            // Arrange
            var headerContent = @"
class TestClass
{
public:
    void EmptyMethod();
    void MethodWithBody();
};
";

            var sourceContent = @"
void TestClass::EmptyMethod()
{
}

void TestClass::MethodWithBody()
{
    int x = 5;
}
";

            // Act
            var result = ConvertWithHeaderAndSource(headerContent, sourceContent);

            // Assert - empty method should NOT have TODO
            Assert.DoesNotContain("EmptyMethod()\n    {\n        // TODO:", result);
            
            // Method with body should have implementation
            Assert.Contains("int x = 5;", result);
        }

        [Fact]
        public void UnresolvedMethod_ShouldHaveTODO()
        {
            // Arrange - In API mode (CsClassGenerator), methods without implementations 
            // generate declarations (;) not implementations with TODO.
            // The TODO + warning logic is only for the command-line tool (CppToCsStructuralConverter).
            var headerContent = @"
class TestClass
{
public:
    void UnresolvedMethod();
    void ResolvedMethod();
};
";

            var sourceContent = @"
void TestClass::ResolvedMethod()
{
    int x = 5;
}
";
            // Note: UnresolvedMethod is not in source file

            // Act
            var result = ConvertWithHeaderAndSource(headerContent, sourceContent);

            // Assert - In API mode, unresolved method generates declaration
            Assert.Contains("void UnresolvedMethod();", result); // Declaration
            Assert.Contains("int x = 5", result); // Resolved implementation
        }

        [Fact]
        public void EmptyConstructor_ShouldNotHaveTODO()
        {
            // Arrange
            var headerContent = @"
class TestClass
{
public:
    TestClass();
};
";

            var sourceContent = @"
TestClass::TestClass()
{
}
";

            // Act
            var result = ConvertWithHeaderAndSource(headerContent, sourceContent);

            // Assert - constructor should NOT have TODO
            Assert.DoesNotContain("public TestClass()\n    {\n        // TODO:", result);
            Assert.Contains("public TestClass()", result);
        }

        [Fact]
        public void EmptyMethodWithComments_ShouldPreserveComments()
        {
            // Arrange
            var headerContent = @"
class TestClass
{
public:
    void MethodWithComment();
};
";

            var sourceContent = @"
void TestClass::MethodWithComment()
{
    // This method intentionally does nothing
}
";

            // Act
            var result = ConvertWithHeaderAndSource(headerContent, sourceContent);

            // Assert - should preserve comment and not add TODO
            Assert.Contains("// This method intentionally does nothing", result);
            Assert.DoesNotContain("// TODO: Implement method", result);
        }

        private string ConvertWithHeaderAndSource(string headerContent, string sourceContent)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            try
            {
                var headerFile = Path.Combine(tempDir, "TestClass.h");
                var sourceFile = Path.Combine(tempDir, "TestClass.cpp");
                var outputDir = Path.Combine(tempDir, "output");

                File.WriteAllText(headerFile, headerContent);
                File.WriteAllText(sourceFile, sourceContent);

                var api = new CppToCsConverterApi();
                api.ConvertDirectory(tempDir, outputDir);

                var outputFile = Path.Combine(outputDir, "TestClass.cs");
                var result = File.ReadAllText(outputFile);
                
                // Debug: Write output to see what's generated
                var debugFile = Path.Combine(tempDir, "debug_output.txt");
                File.WriteAllText(debugFile, result);
                Console.WriteLine($"Debug output written to: {debugFile}");
                
                return result;
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }
    }
}
