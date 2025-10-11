using System.IO;
using System.Linq;
using Xunit;
using CppToCsConverter.Core.Parsers;
using CppToCsConverter.Core.Models;
using CppToCsConverter.Core;
using CppToCsConverter.Core.Utils;
using CppToCsConverter.Core.Core;

namespace CppToCsConverter.Tests
{
    public class LocalMethodTests
    {
        [Fact]
        public void ParseSourceFile_WithSingleLocalMethod_ShouldDetectLocalMethod()
        {
            // Arrange
            var sourceContent = @"
#include ""CSample.h""

// A comment for the local method
bool CheckSomeValue(/* IN */ const agrint& value)
{
    // Method body
    return value > 0 && value < 1000;
}

void CSample::MethodOne()
{
    if (CheckSomeValue(3))
        m_value1 = 1;
}";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, sourceContent);

            try
            {
                // Act
                var parser = new CppSourceParser();
                var sourceFile = parser.ParseSourceFileComplete(tempFile);



                // Assert
                Assert.Equal(2, sourceFile.Methods.Count);
                
                // Check class method
                var classMethod = sourceFile.Methods.FirstOrDefault(m => m.Name == "MethodOne");
                Assert.NotNull(classMethod);
                Assert.False(classMethod.IsLocalMethod);
                Assert.Equal("CSample", classMethod.ClassName);
                
                // Check local method
                var localMethod = sourceFile.Methods.FirstOrDefault(m => m.Name == "CheckSomeValue");
                Assert.NotNull(localMethod);
                Assert.True(localMethod.IsLocalMethod);
                Assert.True(localMethod.IsStatic);
                Assert.Equal("bool", localMethod.ReturnType);
                Assert.Equal("CSample", localMethod.ClassName); // Should be associated with the class from the file
                Assert.Single(localMethod.Parameters);
                Assert.Equal("value", localMethod.Parameters[0].Name);
                
                // Check ordering - local method should come first (order in file)
                Assert.Equal(0, localMethod.OrderIndex);
                Assert.Equal(1, classMethod.OrderIndex);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact] 
        public void ParseSourceFile_WithMultipleLocalMethods_ShouldPreserveOrder()
        {
            // Arrange
            var sourceContent = @"
#include ""CSample.h""

bool CheckSomeValue(const agrint& value)
{
    return value > 0 && value < 1000;
}

void CSample::MethodOne()
{
    m_value1 = 1;
}

// A comment for the local method 2
bool CheckSomeValue2(/* IN */ const agrint& value)
{
    // Method body
    return value > 0 && value < 1000;
}

void CSample::MethodTwo()
{
    m_value1 = 2;
}";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, sourceContent);

            try
            {
                // Act
                var parser = new CppSourceParser();
                var sourceFile = parser.ParseSourceFileComplete(tempFile);

                // Assert
                Assert.Equal(4, sourceFile.Methods.Count);
                
                // Verify ordering matches the file
                Assert.Equal("CheckSomeValue", sourceFile.Methods[0].Name);
                Assert.True(sourceFile.Methods[0].IsLocalMethod);
                Assert.Equal(0, sourceFile.Methods[0].OrderIndex);
                
                Assert.Equal("MethodOne", sourceFile.Methods[1].Name);
                Assert.False(sourceFile.Methods[1].IsLocalMethod);
                Assert.Equal(1, sourceFile.Methods[1].OrderIndex);
                
                Assert.Equal("CheckSomeValue2", sourceFile.Methods[2].Name);
                Assert.True(sourceFile.Methods[2].IsLocalMethod);
                Assert.Equal(2, sourceFile.Methods[2].OrderIndex);
                
                Assert.Equal("MethodTwo", sourceFile.Methods[3].Name);
                Assert.False(sourceFile.Methods[3].IsLocalMethod);
                Assert.Equal(3, sourceFile.Methods[3].OrderIndex);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseSourceFile_WithLocalMethodOverloads_ShouldDetectBoth()
        {
            // Arrange
            var sourceContent = @"
#include ""CSample.h""

bool CheckValue(const agrint& value)
{
    return value > 0;
}

bool CheckValue(const agrint& value, bool strict)
{
    return strict ? value > 10 : value > 0;
}

void CSample::MethodOne()
{
    m_value1 = 1;
}";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, sourceContent);

            try
            {
                // Act
                var parser = new CppSourceParser();
                var sourceFile = parser.ParseSourceFileComplete(tempFile);

                // Assert
                Assert.Equal(3, sourceFile.Methods.Count);
                
                var localMethods = sourceFile.Methods.Where(m => m.IsLocalMethod).ToList();
                Assert.Equal(2, localMethods.Count);
                Assert.All(localMethods, m => Assert.Equal("CheckValue", m.Name));
                Assert.All(localMethods, m => Assert.True(m.IsStatic));
                
                // Check overload differentiation by parameter count
                var singleParamOverload = localMethods.FirstOrDefault(m => m.Parameters.Count == 1);
                var doubleParamOverload = localMethods.FirstOrDefault(m => m.Parameters.Count == 2);
                
                Assert.NotNull(singleParamOverload);
                Assert.NotNull(doubleParamOverload);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseSourceFile_WithForwardDeclaration_ShouldIgnoreForwardDeclaration()
        {
            // Arrange
            var sourceContent = @"
#include ""CSample.h""

// Forward declaration - should be ignored
bool CheckSomeValue(const agrint& value);

void CSample::MethodOne()
{
    m_value1 = 1;
}

// Implementation - should be detected
bool CheckSomeValue(const agrint& value)
{
    return value > 0 && value < 1000;
}";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, sourceContent);

            try
            {
                // Act
                var parser = new CppSourceParser();
                var sourceFile = parser.ParseSourceFileComplete(tempFile);

                // Assert
                Assert.Equal(2, sourceFile.Methods.Count);
                
                var localMethods = sourceFile.Methods.Where(m => m.IsLocalMethod).ToList();
                Assert.Single(localMethods);
                Assert.Equal("CheckSomeValue", localMethods[0].Name);
                Assert.NotEmpty(localMethods[0].ImplementationBody);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        // TODO: Add integration test for generation once local method parsing is implemented
    }
}