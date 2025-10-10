using System;
using System.IO;
using System.Linq;
using CppToCsConverter.Core.Core;
using Xunit;

namespace CppToCsConverter.Tests
{
    public class PartialClassMemberPostfixCommentTests
    {
        [Fact]
        public void GeneratePartialClass_WithMemberPostfixComments_ShouldIncludeInMainClass()
        {
            // Arrange - Create a test scenario that generates partial classes
            var headerContent = @"
class CSample : public ISample
{
private:
    CAgrMT* m_pmtReport; //Res/Rate-Reporting (To do: Not touch?)
    agrint m_value1; // Simple comment
public:
    void MethodOne();
    void MethodTwo();
};";

            var sourceContent1 = @"
#include ""CSample.h""

void CSample::MethodOne()
{
    // Implementation of MethodOne
}";

            var sourceContent2 = @"
#include ""CSample.h""

void CSample::MethodTwo()
{
    // Implementation of MethodTwo
}";

            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            
            var headerFile = Path.Combine(tempDir, "CSample.h");
            var sourceFile1 = Path.Combine(tempDir, "CSample.cpp");
            var sourceFile2 = Path.Combine(tempDir, "CSample_File2.cpp");
            var outputDir = tempDir;

            try
            {
                File.WriteAllText(headerFile, headerContent);
                File.WriteAllText(sourceFile1, sourceContent1);
                File.WriteAllText(sourceFile2, sourceContent2);

                // Act
                var converter = new CppToCsStructuralConverter();
                converter.ConvertFiles(new[] { headerFile }, new[] { sourceFile1, sourceFile2 }, outputDir);

                // Assert - Check the main class file includes member postfix comments
                var mainClassFile = Path.Combine(outputDir, "CSample.cs");
                Assert.True(File.Exists(mainClassFile), $"Main class file should exist: {mainClassFile}");
                
                var mainClassContent = File.ReadAllText(mainClassFile);
                Console.WriteLine($"Main class content:\n{mainClassContent}");
                
                // Should contain member postfix comments in the main partial class
                Assert.Contains("//Res/Rate-Reporting (To do: Not touch?)", mainClassContent);
                Assert.Contains("// Simple comment", mainClassContent);
                
                // Should also contain member declarations
                Assert.Contains("CAgrMT* m_pmtReport", mainClassContent);
                Assert.Contains("agrint m_value1", mainClassContent);
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
        public void GenerateNonPartialClass_WithMemberPostfixComments_ShouldIncludeInOutput()
        {
            // Arrange - Create a test scenario that generates a single (non-partial) class
            var headerContent = @"
class CSample : public ISample
{
private:
    CAgrMT* m_pmtReport; //Res/Rate-Reporting (To do: Not touch?)
    agrint m_value1; // Simple comment
public:
    void MethodOne();
};";

            var sourceContent = @"
#include ""CSample.h""

void CSample::MethodOne()
{
    // Implementation of MethodOne
}";

            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            
            var headerFile = Path.Combine(tempDir, "CSample.h");
            var sourceFile = Path.Combine(tempDir, "CSample.cpp");
            var outputDir = tempDir;

            try
            {
                File.WriteAllText(headerFile, headerContent);
                File.WriteAllText(sourceFile, sourceContent);

                // Act
                var converter = new CppToCsStructuralConverter();
                converter.ConvertFiles(new[] { headerFile }, new[] { sourceFile }, outputDir);

                // Assert - Check the single class file includes member postfix comments
                var classFile = Path.Combine(outputDir, "CSample.cs");
                Assert.True(File.Exists(classFile), $"Class file should exist: {classFile}");
                
                var classContent = File.ReadAllText(classFile);
                Console.WriteLine($"Non-partial class content:\n{classContent}");
                
                // Should contain member postfix comments (this should work)
                Assert.Contains("//Res/Rate-Reporting (To do: Not touch?)", classContent);
                Assert.Contains("// Simple comment", classContent);
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