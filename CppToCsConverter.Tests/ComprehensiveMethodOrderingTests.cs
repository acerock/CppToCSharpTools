using System;
using System.IO;
using System.Linq;
using Xunit;
using CppToCsConverter.Core;

namespace CppToCsConverter.Tests
{
    public class ComprehensiveMethodOrderingTests
    {
        private readonly CppToCsConverterApi _converter;
        private readonly string _tempDir;

        public ComprehensiveMethodOrderingTests()
        {
            _converter = new CppToCsConverterApi();
            _tempDir = Path.GetTempPath();
        }

        [Fact]
        public void PartialClass_AdditionalFile_ShouldRespectSourceFileOrder()
        {
            // Arrange
            var headerContent = @"
#pragma once
class PartialOrderTest
{
public:
    void HeaderMethodA();
    void HeaderMethodB();  
    void HeaderMethodC();
};";

            var sourceContent = @"
#include ""PartialOrderTest.h""

// Methods intentionally in different order than header
void PartialOrderTest::HeaderMethodC()
{
    // Third method implementation
}

void PartialOrderTest::HeaderMethodA()
{
    // First method implementation  
}

void PartialOrderTest::HeaderMethodB()
{
    // Second method implementation
}";

            var tempHeader = Path.Combine(_tempDir, "PartialOrderTest.h");
            var tempMainSource = Path.Combine(_tempDir, "PartialOrderTest.cpp");
            var tempAdditionalSource = Path.Combine(_tempDir, "PartialOrderTestMethods.cpp");
            var outputDir = Path.Combine(_tempDir, Guid.NewGuid().ToString());

            // Create a minimal main .cpp file to trigger partial class detection
            var mainSourceContent = @"#include ""PartialOrderTest.h""
// Main file - empty for this test";

            File.WriteAllText(tempHeader, headerContent);
            File.WriteAllText(tempMainSource, mainSourceContent);
            File.WriteAllText(tempAdditionalSource, sourceContent);

            // Act
            _converter.ConvertSpecificFiles(_tempDir, new[] { "PartialOrderTest.h", "PartialOrderTest.cpp", "PartialOrderTestMethods.cpp" }, outputDir);

            // Assert - Check if partial class was created, otherwise check main file
            var additionalFile = Path.Combine(outputDir, "PartialOrderTestMethods.cs");
            var mainFile = Path.Combine(outputDir, "PartialOrderTest.cs");
            
            string content;
            if (File.Exists(additionalFile))
            {
                content = File.ReadAllText(additionalFile);
            }
            else
            {
                Assert.True(File.Exists(mainFile), "Neither additional nor main file exists");
                content = File.ReadAllText(mainFile);
            }

            var methodAIndex = content.IndexOf("public void HeaderMethodA()");
            var methodBIndex = content.IndexOf("public void HeaderMethodB()");
            var methodCIndex = content.IndexOf("public void HeaderMethodC()");

            Assert.True(methodAIndex > 0);
            Assert.True(methodBIndex > 0);
            Assert.True(methodCIndex > 0);

            // Should be in source file order: C, A, B
            Assert.True(methodCIndex < methodAIndex, "HeaderMethodC should come before HeaderMethodA (source order)");
            Assert.True(methodAIndex < methodBIndex, "HeaderMethodA should come before HeaderMethodB (source order)");

            // Cleanup
            Directory.Delete(outputDir, true);
            File.Delete(tempHeader);
            File.Delete(tempMainSource);
            File.Delete(tempAdditionalSource);
        }

        [Fact]
        public void PartialClass_MainFile_ShouldUseInlineFirstOrdering()
        {
            // Arrange
            var headerContent = @"
#pragma once
class InlineFirstTest
{
private:
    int value;
    
    // First inline method
    bool IsPositive() const 
    {
        return value > 0;
    }

public:
    // Constructor declaration (will have implementation)
    InlineFirstTest();
    
    // Second inline method  
    int GetValue() const 
    {
        return value;
    }
    
    // Regular method declaration (will have implementation)
    void ProcessValue();
    
    // Third inline method
    void SetValue(int newValue) 
    {
        value = newValue;
    }
    
    // Destructor declaration (will have implementation)
    ~InlineFirstTest();
};";

            var sourceContent = @"
#include ""InlineFirstTest.h""

// Implementation methods in specific order
InlineFirstTest::InlineFirstTest()
{
    value = 0;
}

InlineFirstTest::~InlineFirstTest()
{
    // Cleanup
}

void InlineFirstTest::ProcessValue()
{
    value *= 2;
}";

            var tempHeader = Path.Combine(_tempDir, "InlineFirstTest.h");
            var tempSource = Path.Combine(_tempDir, "InlineFirstTest.cpp");
            var outputDir = Path.Combine(_tempDir, Guid.NewGuid().ToString());

            File.WriteAllText(tempHeader, headerContent);
            File.WriteAllText(tempSource, sourceContent);

            // Act
            _converter.ConvertSpecificFiles(_tempDir, new[] { "InlineFirstTest.h", "InlineFirstTest.cpp" }, outputDir);

            // Assert - Main file should use inline-first ordering
            var mainFile = Path.Combine(outputDir, "InlineFirstTest.cs");
            Assert.True(File.Exists(mainFile));

            var content = File.ReadAllText(mainFile);
            
            // Find method positions
            var isPositiveIndex = content.IndexOf("private bool IsPositive()");
            var getValueIndex = content.IndexOf("public int GetValue()");
            var setValueIndex = content.IndexOf("public void SetValue(int newValue)");
            var constructorIndex = content.IndexOf("public InlineFirstTest()");
            var destructorIndex = content.IndexOf("public ~InlineFirstTest()");
            var processValueIndex = content.IndexOf("public void ProcessValue()");

            // Verify all methods are found
            Assert.True(isPositiveIndex > 0, "IsPositive method not found");
            Assert.True(getValueIndex > 0, "GetValue method not found");
            Assert.True(setValueIndex > 0, "SetValue method not found");
            Assert.True(constructorIndex > 0, "Constructor not found");
            Assert.True(destructorIndex > 0, "Destructor not found");
            Assert.True(processValueIndex > 0, "ProcessValue method not found");

            // Inline methods should come first (in header order)
            Assert.True(isPositiveIndex < getValueIndex, "IsPositive should come before GetValue (header order)");
            Assert.True(getValueIndex < setValueIndex, "GetValue should come before SetValue (header order)");
            
            // All inline methods should come before implementation methods
            Assert.True(setValueIndex < constructorIndex, "Last inline method should come before first implementation method");
            
            // Implementation methods should be in source order
            Assert.True(constructorIndex < destructorIndex, "Constructor should come before Destructor (source order)");
            Assert.True(destructorIndex < processValueIndex, "Destructor should come before ProcessValue (source order)");

            // Cleanup
            Directory.Delete(outputDir, true);
            File.Delete(tempHeader);
            File.Delete(tempSource);
        }

        [Fact]
        public void SingleClass_ShouldRespectSourceFileOrder()
        {
            // Arrange
            var headerContent = @"
#pragma once
class SingleOrderTest
{
public:
    void MethodAlpha();
    void MethodBeta();
    void MethodGamma();
};";

            var sourceContent = @"
#include ""SingleOrderTest.h""

// Methods in different order than header
void SingleOrderTest::MethodGamma()
{
    // Third method
}

void SingleOrderTest::MethodAlpha()
{
    // First method
}

void SingleOrderTest::MethodBeta()
{
    // Second method
}";

            var tempHeader = Path.Combine(_tempDir, "SingleOrderTest.h");
            var tempSource = Path.Combine(_tempDir, "SingleOrderTest.cpp");
            var outputDir = Path.Combine(_tempDir, Guid.NewGuid().ToString());

            File.WriteAllText(tempHeader, headerContent);
            File.WriteAllText(tempSource, sourceContent);

            // Act
            _converter.ConvertSpecificFiles(_tempDir, new[] { "SingleOrderTest.h", "SingleOrderTest.cpp" }, outputDir);

            // Assert - Single class should follow source file order
            var outputFile = Path.Combine(outputDir, "SingleOrderTest.cs");
            Assert.True(File.Exists(outputFile));

            var content = File.ReadAllText(outputFile);
            var methodAlphaIndex = content.IndexOf("public void MethodAlpha()");
            var methodBetaIndex = content.IndexOf("public void MethodBeta()");
            var methodGammaIndex = content.IndexOf("public void MethodGamma()");

            Assert.True(methodAlphaIndex > 0);
            Assert.True(methodBetaIndex > 0);
            Assert.True(methodGammaIndex > 0);

            // Should be in source file order: Gamma, Alpha, Beta
            Assert.True(methodGammaIndex < methodAlphaIndex, "MethodGamma should come before MethodAlpha (source order)");
            Assert.True(methodAlphaIndex < methodBetaIndex, "MethodAlpha should come before MethodBeta (source order)");

            // Cleanup
            Directory.Delete(outputDir, true);
            File.Delete(tempHeader);
            File.Delete(tempSource);
        }

        [Fact]
        public void MixedInlineAndImplementation_ShouldSeparateCorrectly()
        {
            // Arrange
            var headerContent = @"
#pragma once
class MixedOrderTest
{
private:
    int value1;
    int value2;
    
public:
    // Inline method A
    int GetValue1() const { return value1; }
    
    // Declaration for implementation
    void SetBoth(int v1, int v2);
    
    // Inline method B
    int GetValue2() const { return value2; }
    
    // Another declaration for implementation
    void Reset();
    
    // Inline method C
    int GetSum() const { return value1 + value2; }
};";

            var sourceContent = @"
#include ""MixedOrderTest.h""

// Implementation in specific order
void MixedOrderTest::Reset()
{
    value1 = 0;
    value2 = 0;
}

void MixedOrderTest::SetBoth(int v1, int v2)
{
    value1 = v1;
    value2 = v2;
}";

            var tempHeader = Path.Combine(_tempDir, "MixedOrderTest.h");
            var tempSource = Path.Combine(_tempDir, "MixedOrderTest.cpp");
            var outputDir = Path.Combine(_tempDir, Guid.NewGuid().ToString());

            File.WriteAllText(tempHeader, headerContent);
            File.WriteAllText(tempSource, sourceContent);

            // Act
            _converter.ConvertSpecificFiles(_tempDir, new[] { "MixedOrderTest.h", "MixedOrderTest.cpp" }, outputDir);

            // Assert
            var outputFile = Path.Combine(outputDir, "MixedOrderTest.cs");
            Assert.True(File.Exists(outputFile));

            var content = File.ReadAllText(outputFile);
            
            // Find method positions
            var getValue1Index = content.IndexOf("public int GetValue1()");
            var getValue2Index = content.IndexOf("public int GetValue2()");  
            var getSumIndex = content.IndexOf("public int GetSum()");
            var setBothIndex = content.IndexOf("public void SetBoth(int v1, int v2)");
            var resetIndex = content.IndexOf("public void Reset()");

            // Verify all methods found
            Assert.True(getValue1Index > 0, "GetValue1 not found");
            Assert.True(getValue2Index > 0, "GetValue2 not found");
            Assert.True(getSumIndex > 0, "GetSum not found");
            Assert.True(setBothIndex > 0, "SetBoth not found");
            Assert.True(resetIndex > 0, "Reset not found");

            // Inline methods should come first (in header order)
            Assert.True(getValue1Index < getValue2Index, "GetValue1 should come before GetValue2 (header order)");
            Assert.True(getValue2Index < getSumIndex, "GetValue2 should come before GetSum (header order)");
            
            // All inline methods should come before implementation methods
            Assert.True(getSumIndex < resetIndex, "Last inline method should come before first implementation method");
            Assert.True(getSumIndex < setBothIndex, "Last inline method should come before all implementation methods");
            
            // Implementation methods should be in source order
            Assert.True(resetIndex < setBothIndex, "Reset should come before SetBoth (source order)");

            // Cleanup
            Directory.Delete(outputDir, true);
            File.Delete(tempHeader);
            File.Delete(tempSource);
        }

        [Fact]
        public void HeaderOnlyClass_ShouldRespectHeaderOrder()
        {
            // Arrange - Header-only class with inline methods only
            var headerContent = @"
#pragma once
class HeaderOnlyTest
{
private:
    int value = 0;
    
public:
    // Method C (first in header)
    void SetValue(int v) { value = v; }
    
    // Method A (second in header)  
    int GetValue() const { return value; }
    
    // Method B (third in header)
    bool IsValid() const { return value >= 0; }
};";

            var tempHeader = Path.Combine(_tempDir, "HeaderOnlyTest.h");
            var outputDir = Path.Combine(_tempDir, Guid.NewGuid().ToString());

            File.WriteAllText(tempHeader, headerContent);

            // Act
            _converter.ConvertSpecificFiles(_tempDir, new[] { "HeaderOnlyTest.h" }, outputDir);

            // Assert - Header-only should maintain header declaration order
            var outputFile = Path.Combine(outputDir, "HeaderOnlyTest.cs");
            Assert.True(File.Exists(outputFile));

            var content = File.ReadAllText(outputFile);
            var setValueIndex = content.IndexOf("public void SetValue(int v)");
            var getValueIndex = content.IndexOf("public int GetValue()");
            var isValidIndex = content.IndexOf("public bool IsValid()");

            Assert.True(setValueIndex > 0, "SetValue not found");
            Assert.True(getValueIndex > 0, "GetValue not found");
            Assert.True(isValidIndex > 0, "IsValid not found");

            // Should maintain header declaration order
            Assert.True(setValueIndex < getValueIndex, "SetValue should come before GetValue (header order)");
            Assert.True(getValueIndex < isValidIndex, "GetValue should come before IsValid (header order)");

            // Cleanup
            Directory.Delete(outputDir, true);
            File.Delete(tempHeader);
        }
    }
}