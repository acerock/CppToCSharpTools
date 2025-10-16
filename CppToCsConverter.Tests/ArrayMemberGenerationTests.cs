using System;
using System.IO;
using System.Linq;
using CppToCsConverter.Core.Core;
using CppToCsConverter.Core.Parsers;
using Xunit;

namespace CppToCsConverter.Tests
{
    /// <summary>
    /// Tests for array member generation based on readme.md requirements.
    /// Tests the conversion of C++ fixed-sized array members to C# array members with initialization.
    /// </summary>
    public class ArrayMemberGenerationTests
    {
        private readonly CppHeaderParser _headerParser;
        private readonly CppToCsStructuralConverter _converter;

        public ArrayMemberGenerationTests()
        {
            _headerParser = new CppHeaderParser();
            _converter = new CppToCsStructuralConverter();
        }

        [Fact]
        public void GenerateArrayMember_WithFixedSize_ShouldCreateInitializedArray()
        {
            // Arrange - Based on readme.md example: agrint m_value1[ARR_SIZE];
            var headerContent = @"
// We move this to the class and another tool will translate it to internal const int ARR_SIZE = 10;
#define ARR_SIZE 10

class CSample : public ISample
{
private:
    // Array of ints
    agrint m_value1[ARR_SIZE]; // Comment about ARR_SIZE 
};";

            var tempDir = Path.GetTempPath();
            var outputDir = Path.Combine(tempDir, Guid.NewGuid().ToString());
            var headerFile = Path.Combine(tempDir, "CSample.h");
            File.WriteAllText(headerFile, headerContent);

            try
            {
                // Act
                _converter.ConvertSpecificFiles(tempDir, new[] { "CSample.h" }, outputDir);

                // Assert
                var outputFile = Path.Combine(outputDir, "CSample.cs");
                Assert.True(File.Exists(outputFile));
                
                var generatedContent = File.ReadAllText(outputFile);
                Console.WriteLine("Generated C# content:");
                Console.WriteLine(generatedContent);
                
                // Check expectations based on readme.md
                Assert.Contains("agrint[] m_value1 = new agrint[ARR_SIZE]; // Comment about ARR_SIZE", generatedContent);
                Assert.Contains("#define ARR_SIZE 10", generatedContent);
            }
            finally
            {
                if (File.Exists(headerFile)) File.Delete(headerFile);
                if (Directory.Exists(outputDir)) Directory.Delete(outputDir, true);
            }
        }

        [Fact]
        public void ParseArrayMember_WithConstantSize_ShouldCaptureArrayInfo()
        {
            // Arrange - Array member with constant size
            var headerContent = @"
class CSample
{
private:
    // Array of ints
    agrint m_value1[10]; // Simple array
    CString m_stringArray[5]; // String array
};";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, headerContent);

            try
            {
                // Act
                var classes = _headerParser.ParseHeaderFile(tempFile);

                // Assert
                Assert.Single(classes);
                var cppClass = classes[0];
                Assert.Equal(2, cppClass.Members.Count);
                
                var intArrayMember = cppClass.Members.FirstOrDefault(m => m.Name == "m_value1");
                Assert.NotNull(intArrayMember);
                Assert.True(intArrayMember.IsArray);
                Assert.Equal("10", intArrayMember.ArraySize);
                Assert.Equal("agrint", intArrayMember.Type);
                Assert.Equal("// Simple array", intArrayMember.PostfixComment);

                var stringArrayMember = cppClass.Members.FirstOrDefault(m => m.Name == "m_stringArray");
                Assert.NotNull(stringArrayMember);
                Assert.True(stringArrayMember.IsArray);
                Assert.Equal("5", stringArrayMember.ArraySize);
                Assert.Equal("CString", stringArrayMember.Type);
                Assert.Equal("// String array", stringArrayMember.PostfixComment);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseArrayMember_WithNamedConstantSize_ShouldCaptureArrayInfo()
        {
            // Arrange - Array member with named constant size (like ARR_SIZE)
            var headerContent = @"
class CSample
{
private:
    // Array with named constant
    agrint m_value1[ARR_SIZE]; // Comment about ARR_SIZE
    CString m_stringArray[MAX_STRINGS]; // String array with named size
};";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, headerContent);

            try
            {
                // Act
                var classes = _headerParser.ParseHeaderFile(tempFile);

                // Assert
                Assert.Single(classes);
                var cppClass = classes[0];
                Assert.Equal(2, cppClass.Members.Count);
                
                var intArrayMember = cppClass.Members.FirstOrDefault(m => m.Name == "m_value1");
                Assert.NotNull(intArrayMember);
                Assert.True(intArrayMember.IsArray);
                Assert.Equal("ARR_SIZE", intArrayMember.ArraySize);
                Assert.Equal("agrint", intArrayMember.Type);
                Assert.Equal("// Comment about ARR_SIZE", intArrayMember.PostfixComment);

                var stringArrayMember = cppClass.Members.FirstOrDefault(m => m.Name == "m_stringArray");
                Assert.NotNull(stringArrayMember);
                Assert.True(stringArrayMember.IsArray);
                Assert.Equal("MAX_STRINGS", stringArrayMember.ArraySize);
                Assert.Equal("CString", stringArrayMember.Type);
                Assert.Equal("// String array with named size", stringArrayMember.PostfixComment);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }
    }
}