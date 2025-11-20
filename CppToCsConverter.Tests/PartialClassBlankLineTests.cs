using Xunit;
using CppToCsConverter.Core.Core;
using System.IO;
using System.Text.RegularExpressions;

namespace CppToCsConverter.Tests
{
    public class PartialClassBlankLineTests
    {
        private readonly CppToCsStructuralConverter _converter;

        public PartialClassBlankLineTests()
        {
            _converter = new CppToCsStructuralConverter();
        }

        [Fact]
        public void PartialClass_WithMultipleMethods_ShouldHaveExactlyOneBlankLineBetweenMethods()
        {
            // Arrange
            string headerContent = @"
class TestClass
{
public:
    void Method1();
    void Method2();
    void Method3();
};";

            string sourceContent = @"
void TestClass::Method1()
{
    int x = 1;
}

void TestClass::Method2()
{
    int y = 2;
}

void TestClass::Method3()
{
    int z = 3;
}";

            var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);

            var headerFile = Path.Combine(tempDir, "TestClass.h");
            var sourceFile = Path.Combine(tempDir, "TestClass.cpp");

            File.WriteAllText(headerFile, headerContent);
            File.WriteAllText(sourceFile, sourceContent);

            try
            {
                // Act
                _converter.ConvertDirectory(tempDir, tempDir);

                // Assert
                var filePath = Path.Combine(tempDir, "TestClass.cs");
                Assert.True(File.Exists(filePath), "TestClass.cs should be generated");
                
                var content = File.ReadAllText(filePath);
                
                // Should NOT have consecutive closing braces (would indicate missing blank lines)
                Assert.DoesNotMatch(new Regex(@"    \}\r?\n    public", RegexOptions.Multiline), content);
                Assert.DoesNotMatch(new Regex(@"    \}\r?\n    private", RegexOptions.Multiline), content);
                
                // Should NOT have double blank lines between methods
                Assert.DoesNotMatch(new Regex(@"    \}\r?\n\r?\n\r?\n    (public|private|protected)", RegexOptions.Multiline), content);
                
                // Should NOT have blank line before class closing brace
                Assert.DoesNotMatch(new Regex(@"\}\r?\n\r?\n\}$", RegexOptions.Multiline), content);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Fact]
        public void PartialClass_WithSingleMethod_ShouldNotHaveBlankLineBeforeClosingBrace()
        {
            // Arrange
            string headerContent = @"
class TestClass
{
public:
    void OnlyMethod();
};";

            string sourceContent = @"
void TestClass::OnlyMethod()
{
    int x = 1;
}";

            var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);

            var headerFile = Path.Combine(tempDir, "TestClass.h");
            var sourceFile = Path.Combine(tempDir, "TestClass.cpp");

            File.WriteAllText(headerFile, headerContent);
            File.WriteAllText(sourceFile, sourceContent);

            try
            {
                // Act
                _converter.ConvertDirectory(tempDir, tempDir);

                // Assert
                var filePath = Path.Combine(tempDir, "TestClass.cs");
                Assert.True(File.Exists(filePath), "TestClass.cs should be generated");
                
                var content = File.ReadAllText(filePath);
                
                // Should NOT have blank line before class closing brace
                Assert.DoesNotMatch(new Regex(@"    \}\r?\n\r?\n\}", RegexOptions.Multiline), content);
                
                // Should end with method closing brace, then class closing brace
                Assert.Matches(new Regex(@"    \}\r?\n\}", RegexOptions.Multiline), content);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Fact]
        public void PartialClass_MultipleFiles_EachShouldHaveCorrectBlankLines()
        {
            // Arrange
            string headerContent = @"
class TestClass
{
public:
    void Method1();
    void Method2();
    void Method3();
    void Method4();
};";

            string sourceContent1 = @"
void TestClass::Method1()
{
    int x = 1;
}

void TestClass::Method2()
{
    int y = 2;
}";

            string sourceContent2 = @"
void TestClass::Method3()
{
    int z = 3;
}

void TestClass::Method4()
{
    int w = 4;
}";

            var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);

            var headerFile = Path.Combine(tempDir, "TestClass.h");
            var source1File = Path.Combine(tempDir, "TestClass.cpp");
            var source2File = Path.Combine(tempDir, "TestClassPart2.cpp");

            File.WriteAllText(headerFile, headerContent);
            File.WriteAllText(source1File, sourceContent1);
            File.WriteAllText(source2File, sourceContent2);

            try
            {
                // Act
                _converter.ConvertDirectory(tempDir, tempDir);

                // Assert - Check first partial file
                var file1Path = Path.Combine(tempDir, "TestClass.cs");
                Assert.True(File.Exists(file1Path), "TestClass.cs should exist");
                
                var content1 = File.ReadAllText(file1Path);
                Assert.Contains("void Method1", content1);
                Assert.Contains("void Method2", content1);
                
                // Should NOT have consecutive closing braces
                Assert.DoesNotMatch(new Regex(@"    \}\r?\n    public", RegexOptions.Multiline), content1);
                
                // Assert - Check second partial file
                var file2Path = Path.Combine(tempDir, "TestClassPart2.cs");
                Assert.True(File.Exists(file2Path), "TestClassPart2.cs should exist");
                
                var content2 = File.ReadAllText(file2Path);
                Assert.Contains("void Method3", content2);
                Assert.Contains("void Method4", content2);
                
                // Should NOT have consecutive closing braces
                Assert.DoesNotMatch(new Regex(@"    \}\r?\n    public", RegexOptions.Multiline), content2);
                
                // Should NOT have blank line before class closing brace in either file
                Assert.DoesNotMatch(new Regex(@"    \}\r?\n\r?\n\}", RegexOptions.Multiline), content1);
                Assert.DoesNotMatch(new Regex(@"    \}\r?\n\r?\n\}", RegexOptions.Multiline), content2);
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
