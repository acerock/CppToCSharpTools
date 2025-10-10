using System.IO;
using System.Linq;
using CppToCsConverter.Core.Models;
using CppToCsConverter.Core.Parsers;
using Xunit;

namespace CppToCsConverter.Tests
{
    public class ParameterCommentPositionBasicTests
    {
        [Fact]
        public void SourceParser_WithPrefixComments_ShouldDetectPrefixPosition()
        {
            // Arrange - Source file with prefix comments (like TrickyToMatch)
            var sourceContent = @"
void TestClass::TestMethod(
    /* IN */ const CString& param1,
    /* OUT */ CAgrMT* param2
)
{
    // Implementation
}";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, sourceContent);

            try
            {
                // Act
                var parser = new CppSourceParser();
                var (methods, _) = parser.ParseSourceFile(tempFile);

                // Assert
                Assert.Single(methods);
                var method = methods[0];
                Assert.Equal(2, method.Parameters.Count);
                
                // First parameter: prefix comment
                var param1 = method.Parameters[0];
                Assert.Single(param1.PositionedComments);
                Assert.Equal(CommentPosition.Prefix, param1.PositionedComments[0].Position);
                Assert.Equal("/* IN */", param1.PositionedComments[0].CommentText);
                
                // Second parameter: prefix comment
                var param2 = method.Parameters[1];
                Assert.Single(param2.PositionedComments);
                Assert.Equal(CommentPosition.Prefix, param2.PositionedComments[0].Position);
                Assert.Equal("/* OUT */", param2.PositionedComments[0].CommentText);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void SourceParser_WithSuffixComments_ShouldDetectSuffixPosition()
        {
            // Arrange - Source file with suffix comments
            var sourceContent = @"
void TestClass::TestMethod(
    const CString& param1 /* IN */,
    CAgrMT* param2 /* OUT */
)
{
    // Implementation
}";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, sourceContent);

            try
            {
                // Act
                var parser = new CppSourceParser();
                var (methods, _) = parser.ParseSourceFile(tempFile);

                // Assert
                Assert.Single(methods);
                var method = methods[0];
                Assert.Equal(2, method.Parameters.Count);
                
                // First parameter: suffix comment
                var param1 = method.Parameters[0];
                Assert.Single(param1.PositionedComments);
                Assert.Equal(CommentPosition.Suffix, param1.PositionedComments[0].Position);
                Assert.Equal("/* IN */", param1.PositionedComments[0].CommentText);
                
                // Second parameter: suffix comment
                var param2 = method.Parameters[1];
                Assert.Single(param2.PositionedComments);
                Assert.Equal(CommentPosition.Suffix, param2.PositionedComments[0].Position);
                Assert.Equal("/* OUT */", param2.PositionedComments[0].CommentText);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void SourceParser_WithMixedComments_ShouldDetectBothPositions()
        {
            // Arrange - Source file with mixed comment positions
            var sourceContent = @"
void TestClass::TestMethod(
    /* IN */ const CString& param1 /* NOT_NULL */,
    const bool& param2 /* IN */
)
{
    // Implementation
}";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, sourceContent);

            try
            {
                // Act
                var parser = new CppSourceParser();
                var (methods, _) = parser.ParseSourceFile(tempFile);

                // Assert
                Assert.Single(methods);
                var method = methods[0];
                Assert.Equal(2, method.Parameters.Count);
                
                // First parameter: both prefix and suffix comments
                var param1 = method.Parameters[0];
                Assert.Equal(2, param1.PositionedComments.Count);
                
                var prefixComment = param1.PositionedComments.FirstOrDefault(c => c.Position == CommentPosition.Prefix);
                Assert.NotNull(prefixComment);
                Assert.Equal("/* IN */", prefixComment.CommentText);
                
                var suffixComment = param1.PositionedComments.FirstOrDefault(c => c.Position == CommentPosition.Suffix);
                Assert.NotNull(suffixComment);
                Assert.Equal("/* NOT_NULL */", suffixComment.CommentText);
                
                // Second parameter: suffix comment only
                var param2 = method.Parameters[1];
                Assert.Single(param2.PositionedComments);
                Assert.Equal(CommentPosition.Suffix, param2.PositionedComments[0].Position);
                Assert.Equal("/* IN */", param2.PositionedComments[0].CommentText);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void BackwardCompatibility_LegacyInlineComments_ShouldStillWork()
        {
            // Arrange - Test that legacy InlineComments are still populated
            var sourceContent = @"
void TestClass::TestMethod(/* IN */ const CString& param1 /* OUT */)
{
    // Implementation
}";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, sourceContent);

            try
            {
                // Act
                var parser = new CppSourceParser();
                var (methods, _) = parser.ParseSourceFile(tempFile);

                // Assert
                Assert.Single(methods);
                var method = methods[0];
                var param = method.Parameters[0];
                
                // PositionedComments should be populated
                Assert.Equal(2, param.PositionedComments.Count);
                
                // Legacy InlineComments should also be populated for backward compatibility
                Assert.Equal(2, param.InlineComments.Count);
                Assert.Contains("/* IN */", param.InlineComments);
                Assert.Contains("/* OUT */", param.InlineComments);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void RealCSample_TrickyToMatch_ShouldParseCorrectly()
        {
            // Arrange - Test the actual CSample.cpp file
            var sampleCppPath = Path.Combine("..", "..", "..", "..", "SamplesAndExpectations", "CSample.cpp");
            
            // Act
            var parser = new CppSourceParser();
            var (methods, _) = parser.ParseSourceFile(sampleCppPath);
            
            // Assert
            var trickyMethod = methods.FirstOrDefault(m => m.Name == "TrickyToMatch");
            Assert.NotNull(trickyMethod);
            Assert.Equal("CSample", trickyMethod.ClassName);
            Assert.Equal(3, trickyMethod.Parameters.Count);
            
            // Verify positioned comments are parsed
            var param1 = trickyMethod.Parameters[0]; // cResTab
            Assert.Equal("cResTab", param1.Name);
            Assert.True(param1.PositionedComments?.Any() ?? false, "First parameter should have positioned comments");
            
            var param2 = trickyMethod.Parameters[1]; // bGetAgeAndTaxNumberFromResTab
            Assert.Equal("bGetAgeAndTaxNumberFromResTab", param2.Name);
            Assert.True(param2.PositionedComments?.Any() ?? false, "Second parameter should have positioned comments");
            
            var param3 = trickyMethod.Parameters[2]; // pmtTable
            Assert.Equal("pmtTable", param3.Name);
            Assert.True(param3.PositionedComments?.Any() ?? false, "Third parameter should have positioned comments");
        }

        [Fact]
        public void StructuralConverter_TrickyToMatch_ShouldGenerateMultiLineWithPositionedComments()
        {
            // Arrange
            // Navigate up from test bin directory to solution root  
            var assemblyLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "";
            var solutionRoot = Path.GetFullPath(Path.Combine(assemblyLocation, "..", "..", "..", ".."));
            var samplesDir = Path.Combine(solutionRoot, "SamplesAndExpectations");
            var outputDir = Path.Combine(Path.GetTempPath(), "TrickyToMatchPositioning");
            
            // Ensure output directory exists
            Directory.CreateDirectory(outputDir);

            // Verify sample files exist
            var headerFile = Path.Combine(samplesDir, "CSample.h");
            var sourceFile = Path.Combine(samplesDir, "CSample.cpp");
            Assert.True(File.Exists(headerFile), $"Header file should exist at {headerFile}");
            Assert.True(File.Exists(sourceFile), $"Source file should exist at {sourceFile}");

            // Act
            var converter = new CppToCsConverter.Core.CppToCsConverterApi();
            converter.ConvertSpecificFiles(samplesDir, new[] { "CSample.h", "CSample.cpp" }, outputDir);

            // Assert
            var generatedFile = Path.Combine(outputDir, "CSample.cs");
            Assert.True(File.Exists(generatedFile), $"Generated file should exist at {generatedFile}");

            var content = File.ReadAllText(generatedFile);
            
            // Verify TrickyToMatch exists and has multi-line format
            Assert.Contains("private void TrickyToMatch(", content);
            
            // Verify positioned comments are preserved without duplication
            Assert.Contains("/* IN*/ const CString& cResTab,", content);
            Assert.Contains("/* IN */ const bool& bGetAgeAndTaxNumberFromResTab,", content);
            Assert.Contains("/* OUT */ CAgrMT* pmtTable)", content);
            
            // Verify no comment duplication (should not have /* IN*/ /* IN*/)
            Assert.DoesNotContain("/* IN*/ /* IN*/", content);
            Assert.DoesNotContain("/* IN */ /* IN */", content);
            Assert.DoesNotContain("/* OUT */ /* OUT */", content);
            
            // Verify multi-line format (parameters on separate lines)
            var lines = content.Split('\n').Select(line => line.Trim()).ToArray();
            var trickyToMatchIndex = Array.FindIndex(lines, line => line.Contains("private void TrickyToMatch("));
            Assert.True(trickyToMatchIndex >= 0, "TrickyToMatch method declaration should be found");
            
            // Next lines should contain the parameters
            Assert.Contains("/* IN*/ const CString& cResTab,", lines[trickyToMatchIndex + 1]);
            Assert.Contains("/* IN */ const bool& bGetAgeAndTaxNumberFromResTab,", lines[trickyToMatchIndex + 2]);
            Assert.Contains("/* OUT */ CAgrMT* pmtTable)", lines[trickyToMatchIndex + 3]);
        }

        [Fact]
        public void FormatCppParameterWithPositionedComments_ShouldNotDuplicateComments()
        {
            // Arrange - Create a parameter with positioned comments
            var param = new CppParameter
            {
                Name = "testParam",
                Type = "const CString",
                IsReference = true,
                PositionedComments = new List<ParameterComment>
                {
                    new ParameterComment { CommentText = "/* IN*/", Position = CommentPosition.Prefix }
                }
            };

            // Act - Call the now-public method directly
            var converter = new CppToCsConverter.Core.Core.CppToCsStructuralConverter();
            var result = converter.FormatCppParameterWithPositionedComments(param);
            Assert.NotNull(result);

            // Assert - Should have comment only once
            Assert.Contains("/* IN*/", result);
            var commentOccurrences = result.Split(new[] { "/* IN*/" }, StringSplitOptions.None).Length - 1;
            Assert.Equal(1, commentOccurrences); // Comment should appear exactly once
            Assert.Contains("const CString& testParam", result);
        }

        [Fact]  
        public void MixedCommentTypes_ShouldPreferPositionedOverInline()
        {
            // Arrange - Parameter with both positioned and inline comments
            var sourceContent = @"
void TestClass::MixedCommentMethod(
    /* POSITIONED */ const CString& param1 /* inline comment */
)
{
    // Implementation  
}";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, sourceContent);

            try
            {
                // Act
                var parser = new CppSourceParser();
                var (methods, _) = parser.ParseSourceFile(tempFile);

                // Assert
                Assert.Single(methods);
                var method = methods[0];
                var param1 = method.Parameters[0];
                
                // Should have both positioned and inline comments
                Assert.NotNull(param1.PositionedComments);
                Assert.NotEmpty(param1.PositionedComments);
                Assert.NotNull(param1.InlineComments);
                Assert.NotEmpty(param1.InlineComments);
                
                // Positioned comment should be detected
                Assert.Equal("/* POSITIONED */", param1.PositionedComments[0].CommentText);
                Assert.Equal(CommentPosition.Prefix, param1.PositionedComments[0].Position);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void EmptyPositionedComments_ShouldFallbackToCleanParameter()
        {
            // Arrange - Parameter without positioned comments
            var param = new CppParameter
            {
                Name = "cleanParam",
                Type = "int",
                IsConst = true,
                DefaultValue = "0"
            };

            // Act - Call the now-public method directly
            var converter = new CppToCsConverter.Core.Core.CppToCsStructuralConverter();
            var result = converter.FormatCppParameterWithPositionedComments(param);
            Assert.NotNull(result);

            // Assert - Should be clean parameter format
            Assert.Equal("const int cleanParam = 0", result);
            Assert.DoesNotContain("/*", result);
            Assert.DoesNotContain("//", result);
        }
    }
}