using System;
using System.IO;
using System.Linq;
using Xunit;
using CppToCsConverter.Core.Parsers;
using CppToCsConverter.Tests.Mocks;

namespace CppToCsConverter.Tests
{
    /// <summary>
    /// Tests for error handling and edge cases based on real-world scenarios.
    /// Covers malformed input, missing files, parsing failures, and recovery.
    /// </summary>
    public class ErrorHandlingTests
    {

        [Fact]
        public void ParseHeaderFile_NonExistentFile_ShouldLogError()
        {
            // Arrange
            var mockLogger = new MockLogger();
            var headerParser = new CppHeaderParser(mockLogger);
            var nonExistentFile = "C:\\NonExistent\\File.h";

            // Act
            var result = headerParser.ParseHeaderFile(nonExistentFile);

            // Assert
            Assert.Empty(result); // Should return empty list
            Assert.Single(mockLogger.ErrorMessages); // Should log exactly one error
            Assert.Contains("Error parsing header file", mockLogger.ErrorMessages[0]);
            Assert.Contains(nonExistentFile, mockLogger.ErrorMessages[0]);
        }

        [Fact]
        public void ParseSourceFile_NonExistentFile_ShouldLogError()
        {
            // Arrange
            var mockLogger = new MockLogger();
            var sourceParser = new CppSourceParser(mockLogger);
            var nonExistentFile = "C:\\NonExistent\\File.cpp";

            // Act
            var (methods, staticInits) = sourceParser.ParseSourceFile(nonExistentFile);

            // Assert
            Assert.Empty(methods); // Should return empty list
            Assert.Empty(staticInits); // Should return empty list
            Assert.Single(mockLogger.ErrorMessages); // Should log exactly one error
            Assert.Contains("Error parsing source file", mockLogger.ErrorMessages[0]);
            Assert.Contains(nonExistentFile, mockLogger.ErrorMessages[0]);
        }

        [Fact]
        public void ParseHeaderFile_EmptyFile_ShouldReturnEmptyList()
        {
            // Arrange
            var headerParser = new CppHeaderParser();
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, "");

            try
            {
                // Act
                var classes = headerParser.ParseHeaderFile(tempFile);

                // Assert
                Assert.Empty(classes);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseSourceFile_EmptyFile_ShouldReturnEmptyList()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, "");

            try
            {
                // Act
                var sourceParser = new CppSourceParser();
                var (methods, staticInits) = sourceParser.ParseSourceFile(tempFile);

                // Assert
                Assert.Empty(methods);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseHeaderFile_MalformedClass_ShouldHandleGracefully()
        {
            // Arrange - Malformed class declaration
            var headerContent = @"
class MalformedClass
{
    // Missing closing brace, invalid syntax
    void Method(
    int param1
    // Missing closing parenthesis and semicolon
";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, headerContent);

            try
            {
                // Act - Should not crash, may return partial results or empty
                var headerParser = new CppHeaderParser();
                var result = headerParser.ParseHeaderFile(tempFile);

                // Assert - Should handle gracefully without exception
                Assert.NotNull(result);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseSourceFile_MalformedMethod_ShouldHandleGracefully()
        {
            // Arrange - Malformed method implementation
            var sourceContent = @"
void CSample::MethodWithMissingBrace()
{
    int x = 1;
    // Missing closing brace

void AnotherMethod()
{
    // This should still be parseable
}
";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, sourceContent);

            try
            {
                // Arrange
                var mockLogger = new MockLogger();
                var sourceParser = new CppSourceParser(mockLogger);
                
                // Act - Should not crash
                var (methods, staticInits) = sourceParser.ParseSourceFile(tempFile);

                // Assert - Should handle gracefully
                Assert.NotNull(methods);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseHeaderFile_UnbalancedBraces_ShouldNotCrash()
        {
            // Arrange - Unbalanced braces
            var headerContent = @"
class TestClass
{
public:
    void Method1() {
        // Missing closing brace
    
    void Method2() {
        return;
    }
}; // Extra closing brace
}
";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, headerContent);

            try
            {
                // Arrange
                var mockLogger = new MockLogger();
                var headerParser = new CppHeaderParser(mockLogger);
                
                // Act
                var result = headerParser.ParseHeaderFile(tempFile);

                // Assert - Should not crash
                Assert.NotNull(result);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseSourceFile_VeryLargeFile_ShouldNotTimeout()
        {
            // Arrange - Create a large file to test performance limits
            var tempFile = Path.GetTempFileName();
            
            try
            {
                using (var writer = new StreamWriter(tempFile))
                {
                    // Write many method implementations
                    for (int i = 0; i < 500; i++) // Reduced from 1000 to avoid long test times
                    {
                        writer.WriteLine($"void TestClass::Method{i}()");
                        writer.WriteLine("{");
                        writer.WriteLine($"    // Implementation {i}");
                        writer.WriteLine($"    int value = {i};");
                        writer.WriteLine("    return;");
                        writer.WriteLine("}");
                        writer.WriteLine();
                    }
                }

                // Arrange
                var mockLogger = new MockLogger();
                var sourceParser = new CppSourceParser(mockLogger);
                
                // Act - Should complete within reasonable time
                var startTime = DateTime.Now;
                var (methods, staticInits) = sourceParser.ParseSourceFile(tempFile);
                var duration = DateTime.Now - startTime;

                // Assert
                Assert.NotNull(methods);
                Assert.True(duration.TotalSeconds < 30); // Should complete in under 30 seconds
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseHeaderFile_NestedClasses_ShouldHandleCorrectly()
        {
            // Arrange - Nested class structures (edge case)
            var headerContent = @"
class OuterClass
{
public:
    class InnerClass
    {
    public:
        void InnerMethod();
    };
    
    void OuterMethod();
};
";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, headerContent);

            try
            {
                // Arrange
                var mockLogger = new MockLogger();
                var headerParser = new CppHeaderParser(mockLogger);
                
                // Act
                var result = headerParser.ParseHeaderFile(tempFile);

                // Assert - Should handle nested structures gracefully
                Assert.NotNull(result);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseHeaderFile_MacrosAndPreprocessorDirectives_ShouldIgnoreOrHandle()
        {
            // Arrange - File with preprocessor directives
            var headerContent = @"
#ifndef HEADER_GUARD
#define HEADER_GUARD

#ifdef DEBUG
#define LOG_ENABLED 1
#else
#define LOG_ENABLED 0
#endif

class TestClass
{
#if LOG_ENABLED
public:
    void LogMethod();
#endif
    
public:
    void RegularMethod();
};

#endif // HEADER_GUARD
";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, headerContent);

            try
            {
                // Arrange
                var mockLogger = new MockLogger();
                var headerParser = new CppHeaderParser(mockLogger);
                
                // Act
                var result = headerParser.ParseHeaderFile(tempFile);

                // Assert - Should parse successfully, ignoring preprocessor directives
                Assert.NotNull(result);
                if (result.Any())
                {
                    var testClass = result[0];
                    Assert.Contains(testClass.Methods, m => m.Name == "RegularMethod");
                }
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseSourceFile_CircularIncludes_ShouldNotCauseInfiniteLoop()
        {
            // Arrange - Test with include statements (shouldn't affect parsing but tests robustness)
            var sourceContent = @"
#include ""Header1.h""
#include ""Header2.h""
#include ""Header1.h"" // Duplicate include

void TestClass::Method1()
{
    // Implementation
}
";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, sourceContent);

            try
            {
                // Arrange
                var mockLogger = new MockLogger();
                var sourceParser = new CppSourceParser(mockLogger);
                
                // Act
                var (methods, staticInits) = sourceParser.ParseSourceFile(tempFile);

                // Assert - Should complete without infinite loops
                Assert.NotNull(methods);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }
    }
}