using Xunit;
using CppToCsConverter.Core;
using System.IO;

namespace CppToCsConverter.Tests
{
    public class ClassClosingBraceTests
    {
        [Fact]
        public void ClassWithMethods_ShouldNotHaveBlankLineBeforeClosingBrace()
        {
            // Arrange
            var headerContent = @"
class TestClass
{
public:
    void MethodOne();
    void MethodTwo();
};
";

            var sourceContent = @"
void TestClass::MethodOne()
{
    int x = 1;
}

void TestClass::MethodTwo()
{
    int y = 2;
}
";

            // Act
            var result = ConvertWithHeaderAndSource(headerContent, sourceContent);

            // Assert - no blank line before closing brace (normalize line endings for comparison)
            var normalized = result.Replace("\r\n", "\n");
            Assert.DoesNotContain("    }\n\n}", normalized); // Double newline before }
            Assert.Contains("    }\n}", normalized); // Single newline before }
        }

        [Fact]
        public void ClassWithMembersOnly_ShouldNotHaveBlankLineBeforeClosingBrace()
        {
            // Arrange
            var headerContent = @"
class TestClass
{
private:
    int memberOne;
    int memberTwo;
};
";

            // Act
            var result = ConvertWithHeaderOnly(headerContent);

            // Assert - no blank line before closing brace (normalize line endings for comparison)
            var normalized = result.Replace("\r\n", "\n");
            Assert.DoesNotContain(";\n\n}", normalized); // Double newline after last member
            Assert.Contains(";\n}", normalized); // Single newline after last member
        }

        [Fact]
        public void ClassWithMembersAndMethods_ShouldNotHaveBlankLineBeforeClosingBrace()
        {
            // Arrange
            var headerContent = @"
class TestClass
{
private:
    int member;
public:
    void Method();
};
";

            var sourceContent = @"
void TestClass::Method()
{
    // Implementation
}
";

            // Act
            var result = ConvertWithHeaderAndSource(headerContent, sourceContent);

            // Assert - no blank line before closing brace (normalize line endings for comparison)
            var normalized = result.Replace("\r\n", "\n");
            Assert.DoesNotContain("    }\n\n}", normalized); // Double newline before }
            Assert.Contains("    }\n}", normalized); // Single newline before }
        }

        [Fact]
        public void MultipleClasses_ShouldNotHaveBlankLineBeforeAnyClosingBrace()
        {
            // Arrange
            var headerContent = @"
class ClassOne
{
public:
    void MethodOne();
};

class ClassTwo
{
public:
    void MethodTwo();
};
";

            var sourceContent = @"
void ClassOne::MethodOne()
{
    int x = 1;
}

void ClassTwo::MethodTwo()
{
    int y = 2;
}
";

            // Act
            var result = ConvertWithHeaderAndSource(headerContent, sourceContent);

            // Assert - count occurrences of closing braces with single newline (normalize line endings)
            var normalized = result.Replace("\r\n", "\n");
            var singleNewlineBeforeClosing = System.Text.RegularExpressions.Regex.Matches(normalized, @"    \}\n\}").Count;
            Assert.True(singleNewlineBeforeClosing >= 2, "Both classes should have single newline before closing brace");
            
            // Assert - no double newlines before closing braces
            Assert.DoesNotContain("    }\n\n}", normalized);
        }

        private string ConvertWithHeaderOnly(string headerContent)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var outputDir = Path.Combine(tempDir, "output");

            try
            {
                Directory.CreateDirectory(tempDir);
                Directory.CreateDirectory(outputDir);

                var headerFile = Path.Combine(tempDir, "TestClass.h");
                File.WriteAllText(headerFile, headerContent);

                var api = new CppToCsConverterApi();
                api.ConvertDirectory(tempDir, outputDir);

                var outputFile = Path.Combine(outputDir, "TestClass.cs");
                return File.ReadAllText(outputFile);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        private string ConvertWithHeaderAndSource(string headerContent, string sourceContent)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var outputDir = Path.Combine(tempDir, "output");

            try
            {
                Directory.CreateDirectory(tempDir);
                Directory.CreateDirectory(outputDir);

                var headerFile = Path.Combine(tempDir, "TestClass.h");
                var sourceFile = Path.Combine(tempDir, "TestClass.cpp");
                
                File.WriteAllText(headerFile, headerContent);
                File.WriteAllText(sourceFile, sourceContent);

                var api = new CppToCsConverterApi();
                api.ConvertDirectory(tempDir, outputDir);

                var outputFile = Path.Combine(outputDir, "TestClass.cs");
                return File.ReadAllText(outputFile);
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
