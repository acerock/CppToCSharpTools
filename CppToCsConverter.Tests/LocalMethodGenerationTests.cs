using System;
using System.IO;
using System.Linq;
using CppToCsConverter.Core.Core;
using CppToCsConverter.Core.Parsers;
using Xunit;

namespace CppToCsConverter.Tests
{
    public class LocalMethodGenerationTests
    {
        [Fact]
        public void ConvertFiles_WithLocalMethods_ShouldGeneratePrivateStaticMethods()
        {
            // Arrange - Create temp header and source files
            var tempDir = Path.GetTempPath();
            var headerFile = Path.Combine(tempDir, "TestLocalMethod.h");
            var sourceFile = Path.Combine(tempDir, "TestLocalMethod.cpp");
            var outputDir = Path.Combine(tempDir, "LocalMethodOutput");
            
            var headerContent = @"
#pragma once

class TestLocalMethod
{
public:
    void PublicMethod();
    
private:
    int m_value;
};
";

            var sourceContent = @"
#include ""TestLocalMethod.h""

// Local method without class scope regulator
bool ValidateInput(const int& value)
{
    return value > 0 && value < 1000;
}

void TestLocalMethod::PublicMethod()
{
    if (ValidateInput(m_value))
        m_value = 42;
}
";

            try
            {
                // Create temp files
                File.WriteAllText(headerFile, headerContent);
                File.WriteAllText(sourceFile, sourceContent);
                
                if (Directory.Exists(outputDir))
                    Directory.Delete(outputDir, true);
                Directory.CreateDirectory(outputDir);
                
                // Act
                var converter = new CppToCsStructuralConverter();
                converter.ConvertFiles(new[] { headerFile }, new[] { sourceFile }, outputDir);
                
                // Assert - Check generated C# file
                var generatedFile = Path.Combine(outputDir, "TestLocalMethod.cs");
                Assert.True(File.Exists(generatedFile), $"Generated file should exist at {generatedFile}");
                
                var generatedContent = File.ReadAllText(generatedFile);
                
                // Local method should be generated as private static
                Assert.Contains("private static bool ValidateInput", generatedContent);
                Assert.Contains("return value > 0 && value < 1000;", generatedContent);
                
                // Regular class method should still be present
                Assert.Contains("public void PublicMethod", generatedContent);
                Assert.Contains("if (ValidateInput(m_value))", generatedContent);
            }
            finally
            {
                // Cleanup
                if (File.Exists(headerFile)) File.Delete(headerFile);
                if (File.Exists(sourceFile)) File.Delete(sourceFile);
                if (Directory.Exists(outputDir)) Directory.Delete(outputDir, true);
            }
        }
        
        [Fact]
        public void ConvertFiles_WithMultipleLocalMethods_ShouldMaintainOrder()
        {
            // Arrange - Create temp files with multiple local methods
            var tempDir = Path.GetTempPath();
            var headerFile = Path.Combine(tempDir, "MultiLocalMethod.h");
            var sourceFile = Path.Combine(tempDir, "MultiLocalMethod.cpp");
            var outputDir = Path.Combine(tempDir, "MultiLocalMethodOutput");
            
            var headerContent = @"
class MultiLocalMethod
{
public:
    void ProcessData();
};
";

            var sourceContent = @"
#include ""MultiLocalMethod.h""

// First local method
int CalculateValue(int input)
{
    return input * 2;
}

// Second local method  
bool IsValid(int value)
{
    return value > 0;
}

void MultiLocalMethod::ProcessData()
{
    int result = CalculateValue(5);
    if (IsValid(result))
        // Process the result
        ;
}
";

            try
            {
                // Create temp files
                File.WriteAllText(headerFile, headerContent);
                File.WriteAllText(sourceFile, sourceContent);
                
                if (Directory.Exists(outputDir))
                    Directory.Delete(outputDir, true);
                Directory.CreateDirectory(outputDir);
                
                // Act
                var converter = new CppToCsStructuralConverter();
                converter.ConvertFiles(new[] { headerFile }, new[] { sourceFile }, outputDir);
                
                // Assert
                var generatedFile = Path.Combine(outputDir, "MultiLocalMethod.cs");
                Assert.True(File.Exists(generatedFile));
                
                var generatedContent = File.ReadAllText(generatedFile);
                
                // Both local methods should be present as private static
                Assert.Contains("private static int CalculateValue", generatedContent);
                Assert.Contains("private static bool IsValid", generatedContent);
                
                // Check ordering - CalculateValue should appear before IsValid (file order)
                var calculateIndex = generatedContent.IndexOf("private static int CalculateValue");
                var isValidIndex = generatedContent.IndexOf("private static bool IsValid");
                Assert.True(calculateIndex < isValidIndex, "Local methods should maintain file order");
                
                // Class method should still be present
                Assert.Contains("public void ProcessData", generatedContent);
            }
            finally
            {
                // Cleanup
                if (File.Exists(headerFile)) File.Delete(headerFile);
                if (File.Exists(sourceFile)) File.Delete(sourceFile);
                if (Directory.Exists(outputDir)) Directory.Delete(outputDir, true);
            }
        }
        
        [Fact]
        public void ConvertFiles_ComprehensiveLocalMethodDemo_ShouldGenerateCompleteWorkingCode()
        {
            // Arrange - Complete demonstration of local methods feature
            var tempDir = Path.GetTempPath();
            var headerFile = Path.Combine(tempDir, "LocalMethodDemo.h");
            var sourceFile = Path.Combine(tempDir, "LocalMethodDemo.cpp");
            var outputDir = Path.Combine(tempDir, "LocalMethodDemoOutput");
            
            // Create C++ header file
            var headerContent = @"
#pragma once

class LocalMethodDemo
{
public:
    void ProcessData(int value);
    
private:
    int m_result;
};
";

            // Create C++ source file with local methods
            var sourceContent = @"
#include ""LocalMethodDemo.h""

// First local method - validation
bool ValidateInput(const int& value)
{
    return value > 0 && value < 1000;
}

// Second local method - calculation
int CalculateSquare(int input)
{
    return input * input;
}

// Class method implementation that uses local methods
void LocalMethodDemo::ProcessData(int value)
{
    if (ValidateInput(value))
    {
        m_result = CalculateSquare(value);
    }
    else
    {
        m_result = -1;
    }
}
";

            try
            {
                // Create temp files
                File.WriteAllText(headerFile, headerContent);
                File.WriteAllText(sourceFile, sourceContent);
                
                if (Directory.Exists(outputDir))
                    Directory.Delete(outputDir, true);
                Directory.CreateDirectory(outputDir);
                
                // Act
                var converter = new CppToCsStructuralConverter();
                converter.ConvertFiles(new[] { headerFile }, new[] { sourceFile }, outputDir);
                
                // Assert - Check generated C# file
                var generatedFile = Path.Combine(outputDir, "LocalMethodDemo.cs");
                Assert.True(File.Exists(generatedFile), $"Generated file should exist at {generatedFile}");
                
                var generatedContent = File.ReadAllText(generatedFile);
                
                // Comprehensive validation of local methods feature
                
                // 1. Local methods converted to private static
                Assert.Contains("private static bool ValidateInput", generatedContent);
                Assert.Contains("private static int CalculateSquare", generatedContent);
                
                // 2. Method signatures preserved correctly
                Assert.Contains("ValidateInput(const int& value)", generatedContent);
                Assert.Contains("CalculateSquare(int input)", generatedContent);
                
                // 3. Method implementations preserved
                Assert.Contains("return value > 0 && value < 1000;", generatedContent);
                Assert.Contains("return input * input;", generatedContent);
                
                // 4. Class method preserved as public
                Assert.Contains("public void ProcessData(int value)", generatedContent);
                
                // 5. Method calls maintained in implementation
                Assert.Contains("if (ValidateInput(value))", generatedContent);
                Assert.Contains("CalculateSquare(value)", generatedContent);
                
                // 6. Class members preserved
                Assert.Contains("private int m_result;", generatedContent);
                
                // 7. Complete control flow preserved
                Assert.Contains("m_result = CalculateSquare(value);", generatedContent);
                Assert.Contains("m_result = -1;", generatedContent);
                
                // 8. Comments preserved
                Assert.Contains("// Class method implementation that uses local methods", generatedContent);
                
                // 9. Validate ordering - local methods should appear before class method
                var validateIndex = generatedContent.IndexOf("private static bool ValidateInput");
                var calculateIndex = generatedContent.IndexOf("private static int CalculateSquare");
                var processIndex = generatedContent.IndexOf("public void ProcessData");
                
                Assert.True(validateIndex > 0, "ValidateInput method should be found");
                Assert.True(calculateIndex > validateIndex, "CalculateSquare should come after ValidateInput");
                Assert.True(processIndex > calculateIndex, "ProcessData should come after local methods");
                
                // 10. Ensure no duplicate methods or missing functionality
                var validateMatches = System.Text.RegularExpressions.Regex.Matches(generatedContent, @"ValidateInput").Count;
                var calculateMatches = System.Text.RegularExpressions.Regex.Matches(generatedContent, @"CalculateSquare").Count;
                
                // Should appear once in declaration and once in usage (2 times each)
                Assert.True(validateMatches >= 2, $"ValidateInput should appear at least 2 times (declaration + usage), found {validateMatches}");
                Assert.True(calculateMatches >= 2, $"CalculateSquare should appear at least 2 times (declaration + usage), found {calculateMatches}");
            }
            finally
            {
                // Cleanup
                if (File.Exists(headerFile)) File.Delete(headerFile);
                if (File.Exists(sourceFile)) File.Delete(sourceFile);
                if (Directory.Exists(outputDir)) Directory.Delete(outputDir, true);
            }
        }
    }
}