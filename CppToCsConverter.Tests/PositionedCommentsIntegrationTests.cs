using System;
using System.IO;
using System.Linq;
using CppToCsConverter.Core.Models;
using CppToCsConverter.Core.Parsers;
using CppToCsConverter.Core;
using Xunit;

namespace CppToCsConverter.Tests
{
    /// <summary>
    /// Integration tests for positioned comments in both regular classes and partial classes.
    /// Verifies that the positioned comment system works consistently across different class generation approaches.
    /// </summary>
    public class PositionedCommentsIntegrationTests
    {
        private readonly CppHeaderParser _headerParser;
        private readonly CppSourceParser _sourceParser;
        private readonly CppToCsConverterApi _converter;

        public PositionedCommentsIntegrationTests()
        {
            _headerParser = new CppHeaderParser();
            _sourceParser = new CppSourceParser();
            _converter = new CppToCsConverterApi();
        }

        [Fact]
        public void RegularClass_TrickyToMatch_ShouldPreservePositionedComments()
        {
            // Arrange - Use the real SamplesAndExpectations files to test TrickyToMatch
            var samplesDir = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "SamplesAndExpectations");
            var outputDir = Path.Combine(Path.GetTempPath(), "RegularClassPositionedComments");
            
            if (Directory.Exists(outputDir))
                Directory.Delete(outputDir, true);

            try
            {
                // Act - Convert the real samples
                _converter.ConvertDirectory(samplesDir, outputDir);

                // Assert - Check that CSample.cs contains TrickyToMatch with positioned comments
                var csampleFile = Path.Combine(outputDir, "CSample.cs");
                Assert.True(File.Exists(csampleFile), "CSample.cs should be generated");

                var content = File.ReadAllText(csampleFile);
                var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                // Find TrickyToMatch method
                var trickyToMatchIndex = Array.FindIndex(lines, line => line.Contains("private void TrickyToMatch("));
                Assert.True(trickyToMatchIndex >= 0, "TrickyToMatch method should be found in regular class");

                // Verify positioned comments are preserved
                Assert.Contains("/* IN*/ const CString& cResTab,", lines[trickyToMatchIndex + 1]);
                Assert.Contains("/* IN */ const bool& bGetAgeAndTaxNumberFromResTab,", lines[trickyToMatchIndex + 2]);
                Assert.Contains("/* OUT */ CAgrMT* pmtTable)", lines[trickyToMatchIndex + 3]);
            }
            finally
            {
                if (Directory.Exists(outputDir))
                    Directory.Delete(outputDir, true);
            }
        }

        [Fact]
        public void PartialClass_GetRate_ShouldPreservePositionedComments()
        {
            // Arrange - Use the real SamplesAndExpectations files to test GetRate in partial class
            var samplesDir = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "SamplesAndExpectations");
            var outputDir = Path.Combine(Path.GetTempPath(), "PartialClassPositionedComments");
            
            if (Directory.Exists(outputDir))
                Directory.Delete(outputDir, true);

            try
            {
                // Act - Convert the real samples
                _converter.ConvertDirectory(samplesDir, outputDir);

                // Assert - Check that CPartialSampleMethods.cs contains GetRate with positioned comments
                var partialFile = Path.Combine(outputDir, "CPartialSampleMethods.cs");
                Assert.True(File.Exists(partialFile), "CPartialSampleMethods.cs should be generated");

                var content = File.ReadAllText(partialFile);
                var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                // Find GetRate method
                var getRateIndex = Array.FindIndex(lines, line => line.Contains("public agrint GetRate("));
                Assert.True(getRateIndex >= 0, "GetRate method should be found in partial class");

                // Verify positioned comments are preserved (should be multi-line format)
                var methodLines = lines.Skip(getRateIndex).Take(10).ToArray();
                
                // Check for positioned comments in the method parameters
                Assert.True(methodLines.Any(line => line.Contains("/*IN/OUT: Memory table")), 
                    "Should contain positioned comment for dValue parameter");
                Assert.True(methodLines.Any(line => line.Contains("/*OUT: Return value (rate)")), 
                    "Should contain positioned comment for dimValueId parameter");
                Assert.True(methodLines.Any(line => line.Contains("/*IN: Value reference to retrieve")), 
                    "Should contain positioned comment for cTransDateFrom parameter");
            }
            finally
            {
                if (Directory.Exists(outputDir))
                    Directory.Delete(outputDir, true);
            }
        }

        [Fact]
        public void PositionedComments_BothRegularAndPartial_ShouldBeConsistent()
        {
            // Arrange - Use the real SamplesAndExpectations files
            var samplesDir = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "SamplesAndExpectations");
            var outputDir = Path.Combine(Path.GetTempPath(), "ConsistentPositionedComments");
            
            if (Directory.Exists(outputDir))
                Directory.Delete(outputDir, true);

            try
            {
                // Act - Convert the real samples
                _converter.ConvertDirectory(samplesDir, outputDir);

                // Assert - Verify both files exist and have positioned comments
                var csampleFile = Path.Combine(outputDir, "CSample.cs");
                var partialFile = Path.Combine(outputDir, "CPartialSampleMethods.cs");
                
                Assert.True(File.Exists(csampleFile), "CSample.cs should be generated");
                Assert.True(File.Exists(partialFile), "CPartialSampleMethods.cs should be generated");

                var csampleContent = File.ReadAllText(csampleFile);
                var partialContent = File.ReadAllText(partialFile);

                // Verify both have positioned comments
                Assert.Contains("/* IN*/", csampleContent);
                Assert.Contains("/* OUT */", csampleContent);
                Assert.Contains("/*IN/OUT:", partialContent);
                Assert.Contains("/*OUT:", partialContent);
                Assert.Contains("/*IN:", partialContent);

                // Verify method signature formats are multi-line (indicating positioned comments worked)
                Assert.Contains("TrickyToMatch(\n", csampleContent.Replace("\r\n", "\n"));
                Assert.Contains("GetRate(\n", partialContent.Replace("\r\n", "\n"));
            }
            finally
            {
                if (Directory.Exists(outputDir))
                    Directory.Delete(outputDir, true);
            }
        }

        [Fact]
        public void PartialClass_ParameterMerging_ShouldPreferImplementationComments()
        {
            // Arrange - Create test files with different comments in header vs implementation
            var tempDir = Path.GetTempPath();
            var headerFile = Path.Combine(tempDir, "TestPartial.h");
            var implFile = Path.Combine(tempDir, "TestPartialMethods.cpp");
            
            var headerContent = @"
class TestPartial
{
public:
    void TestMethod(const CString& param1, /* header comment */ const bool& param2);
};";

            var implContent = @"
#include ""TestPartial.h""

void TestPartial::TestMethod(
    /* implementation prefix */ const CString& param1,
    /* implementation comment */ const bool& param2
)
{
    // Implementation
}";

            File.WriteAllText(headerFile, headerContent);
            File.WriteAllText(implFile, implContent);

            try
            {
                var outputDir = Path.Combine(Path.GetTempPath(), "ParameterMergingTest");
                if (Directory.Exists(outputDir))
                    Directory.Delete(outputDir, true);

                // Act
                _converter.ConvertDirectory(tempDir, outputDir);

                // Assert - Implementation comments should take precedence
                var files = Directory.GetFiles(outputDir, "*.cs");
                Assert.NotEmpty(files);

                var partialFile = files.FirstOrDefault(f => Path.GetFileName(f).Contains("TestPartialMethods"));
                if (partialFile != null)
                {
                    var content = File.ReadAllText(partialFile);
                    
                    // Should prefer implementation comments over header comments
                    Assert.Contains("/* implementation prefix */", content);
                    Assert.Contains("/* implementation comment */", content);
                    Assert.DoesNotContain("/* header comment */", content);
                }
            }
            finally
            {
                if (File.Exists(headerFile)) File.Delete(headerFile);
                if (File.Exists(implFile)) File.Delete(implFile);
                
                var outputDir = Path.Combine(Path.GetTempPath(), "ParameterMergingTest");
                if (Directory.Exists(outputDir))
                    Directory.Delete(outputDir, true);
            }
        }

        [Fact]
        public void PartialClass_WithoutImplementation_ShouldUseHeaderComments()
        {
            // Arrange - Create test files where partial class method has no implementation
            var tempDir = Path.GetTempPath();
            var headerFile = Path.Combine(tempDir, "TestHeaderOnly.h");
            
            var headerContent = @"
class TestHeaderOnly
{
public:
    void HeaderMethod(/* header only comment */ const CString& param1);
    void InlineMethod(const bool& param2) { /* inline implementation */ }
};";

            File.WriteAllText(headerFile, headerContent);

            try
            {
                var outputDir = Path.Combine(Path.GetTempPath(), "HeaderOnlyTest");
                if (Directory.Exists(outputDir))
                    Directory.Delete(outputDir, true);

                // Act
                _converter.ConvertDirectory(tempDir, outputDir);

                // Assert - Header comments should be preserved when no implementation exists
                var files = Directory.GetFiles(outputDir, "*.cs");
                Assert.NotEmpty(files);

                var content = File.ReadAllText(files[0]);
                
                // Should use header comments when no implementation is available
                Assert.Contains("/* header only comment */", content);
            }
            finally
            {
                if (File.Exists(headerFile)) File.Delete(headerFile);
                
                var outputDir = Path.Combine(Path.GetTempPath(), "HeaderOnlyTest");
                if (Directory.Exists(outputDir))
                    Directory.Delete(outputDir, true);
            }
        }
    }
}