using System.IO;
using System.Linq;
using CppToCsConverter.Core.Models;
using CppToCsConverter.Core.Parsers;
using CppToCsConverter.Core.Core;
using Xunit;

namespace CppToCsConverter.Tests
{
    public class ParameterCommentPositionTests
    {
        [Fact]
        public void PositionedComments_WithPrefixComments_ShouldDetectPrefixPosition()
        {
            // Arrange - Source file with prefix comments (this is known to work like TrickyToMatch)
            var sourceContent = @"
#include ""TestClass.h""

void TestClass::TestMethod(
    /* IN */ const CString& param1,
    /* IN */ const bool& param2,
    /* OUT */ CAgrMT* param3
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
                Assert.Equal(3, method.Parameters.Count);
                
                // All parameters should have prefix comments
                foreach (var param in method.Parameters)
                {
                    Assert.Single(param.PositionedComments);
                    Assert.Equal(CommentPosition.Prefix, param.PositionedComments[0].Position);
                }
                
                // Verify specific comments
                Assert.Equal("/* IN */", method.Parameters[0].PositionedComments[0].CommentText);
                Assert.Equal("/* IN */", method.Parameters[1].PositionedComments[0].CommentText);
                Assert.Equal("/* OUT */", method.Parameters[2].PositionedComments[0].CommentText);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseParameter_WithSuffixComment_ShouldDetectSuffixPosition()
        {
            // Arrange - Parameter with comment after type/name
            var headerContent = @"
class TestClass
{
public:
    void TestMethod(const CString& param1 /* OUT */);
};";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, headerContent);

            try
            {
                // Act
                var parser = new CppHeaderParser();
                var classes = parser.ParseHeaderFile(tempFile);

                // Assert
                Assert.Single(classes);
                var testClass = classes[0];
                Assert.Single(testClass.Methods);
                
                var method = testClass.Methods[0];
                Assert.Single(method.Parameters);
                
                var param = method.Parameters[0];
                Assert.NotNull(param.PositionedComments);
                Assert.Single(param.PositionedComments);
                
                var comment = param.PositionedComments[0];
                Assert.Equal("/* OUT */", comment.CommentText);
                Assert.Equal(CommentPosition.Suffix, comment.Position);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseParameter_WithMixedComments_ShouldDetectBothPositions()
        {
            // Arrange - Parameter with both prefix and suffix comments
            var headerContent = @"
class TestClass
{
public:
    void TestMethod(/* IN */ const CString& param1 /* NOT_NULL */);
};";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, headerContent);

            try
            {
                // Act
                var parser = new CppHeaderParser();
                var classes = parser.ParseHeaderFile(tempFile);

                // Assert
                Assert.Single(classes);
                var testClass = classes[0];
                Assert.Single(testClass.Methods);
                
                var method = testClass.Methods[0];
                Assert.Single(method.Parameters);
                
                var param = method.Parameters[0];
                Assert.NotNull(param.PositionedComments);
                Assert.Equal(2, param.PositionedComments.Count);
                
                // First comment should be prefix
                var prefixComment = param.PositionedComments.FirstOrDefault(c => c.Position == CommentPosition.Prefix);
                Assert.NotNull(prefixComment);
                Assert.Equal("/* IN */", prefixComment.CommentText);
                
                // Second comment should be suffix
                var suffixComment = param.PositionedComments.FirstOrDefault(c => c.Position == CommentPosition.Suffix);
                Assert.NotNull(suffixComment);
                Assert.Equal("/* NOT_NULL */", suffixComment.CommentText);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseMethod_WithMultipleParametersAndMixedComments_ShouldPositionCorrectly()
        {
            // Arrange - Multiple parameters with different comment positions
            var headerContent = @"
class TestClass
{
public:
    void TestMethod(
        /* IN */ const CString& param1,
        const bool& param2 /* IN */,
        /* OUT */ CAgrMT* param3 /* NOT_NULL */,
        int param4 // Simple comment
    );
};";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, headerContent);

            try
            {
                // Act
                var parser = new CppHeaderParser();
                var classes = parser.ParseHeaderFile(tempFile);

                // Assert
                Assert.Single(classes);
                var testClass = classes[0];
                Assert.Single(testClass.Methods);
                
                var method = testClass.Methods[0];
                Assert.Equal(4, method.Parameters.Count);
                
                // Parameter 1: Prefix comment only
                var param1 = method.Parameters[0];
                Assert.Single(param1.PositionedComments);
                Assert.Equal(CommentPosition.Prefix, param1.PositionedComments[0].Position);
                Assert.Equal("/* IN */", param1.PositionedComments[0].CommentText);
                
                // Parameter 2: Suffix comment only
                var param2 = method.Parameters[1];
                Assert.Single(param2.PositionedComments);
                Assert.Equal(CommentPosition.Suffix, param2.PositionedComments[0].Position);
                Assert.Equal("/* IN */", param2.PositionedComments[0].CommentText);
                
                // Parameter 3: Both prefix and suffix comments
                var param3 = method.Parameters[2];
                Assert.Equal(2, param3.PositionedComments.Count);
                var prefixComment = param3.PositionedComments.FirstOrDefault(c => c.Position == CommentPosition.Prefix);
                var suffixComment = param3.PositionedComments.FirstOrDefault(c => c.Position == CommentPosition.Suffix);
                Assert.NotNull(prefixComment);
                Assert.NotNull(suffixComment);
                Assert.Equal("/* OUT */", prefixComment.CommentText);
                Assert.Equal("/* NOT_NULL */", suffixComment.CommentText);
                
                // Parameter 4: Single-line comment (should be suffix)
                var param4 = method.Parameters[3];
                Assert.Single(param4.PositionedComments);
                Assert.Equal(CommentPosition.Suffix, param4.PositionedComments[0].Position);
                Assert.Contains("// Simple comment", param4.PositionedComments[0].CommentText);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseSourceFile_WithPrefixComments_ShouldDetectPrefixPosition()
        {
            // Arrange - Source file with prefix comments (like TrickyToMatch)
            var sourceContent = @"
#include ""TestClass.h""

void TestClass::TestMethod(
    /* IN */ const CString& param1,
    /* IN */ const bool& param2,
    /* OUT */ CAgrMT* param3
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
                Assert.Equal(3, method.Parameters.Count);
                
                // All parameters should have prefix comments
                foreach (var param in method.Parameters)
                {
                    Assert.Single(param.PositionedComments);
                    Assert.Equal(CommentPosition.Prefix, param.PositionedComments[0].Position);
                }
                
                // Verify specific comments
                Assert.Equal("/* IN */", method.Parameters[0].PositionedComments[0].CommentText);
                Assert.Equal("/* IN */", method.Parameters[1].PositionedComments[0].CommentText);
                Assert.Equal("/* OUT */", method.Parameters[2].PositionedComments[0].CommentText);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseSourceFile_WithSuffixComments_ShouldDetectSuffixPosition()
        {
            // Arrange - Source file with suffix comments
            var sourceContent = @"
#include ""TestClass.h""

void TestClass::TestMethod(
    const CString& param1 /* IN */,
    const bool& param2 /* IN */,
    CAgrMT* param3 /* OUT */
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
                Assert.Equal(3, method.Parameters.Count);
                
                // All parameters should have suffix comments
                foreach (var param in method.Parameters)
                {
                    Assert.Single(param.PositionedComments);
                    Assert.Equal(CommentPosition.Suffix, param.PositionedComments[0].Position);
                }
                
                // Verify specific comments
                Assert.Equal("/* IN */", method.Parameters[0].PositionedComments[0].CommentText);
                Assert.Equal("/* IN */", method.Parameters[1].PositionedComments[0].CommentText);
                Assert.Equal("/* OUT */", method.Parameters[2].PositionedComments[0].CommentText);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void GenerateMethodSignature_WithPrefixComments_ShouldPositionCorrectly()
        {
            // Arrange - Method with prefix parameter comments
            var sourceContent = @"
#include ""TestClass.h""

void TestClass::TestMethod(/* IN */ const CString& param1, /* OUT */ CAgrMT* param2)
{
    // Implementation
}";

            var headerContent = @"
class TestClass
{
public:
    void TestMethod(const CString& param1, CAgrMT* param2);
};";

            var tempSourceFile = Path.GetTempFileName();
            var tempHeaderFile = Path.GetTempFileName();
            var outputDir = Path.GetTempPath();

            try
            {
                File.WriteAllText(tempSourceFile, sourceContent);
                File.WriteAllText(tempHeaderFile, headerContent);

                // Act
                var converter = new CppToCsStructuralConverter();
                converter.ConvertFiles(new[] { tempHeaderFile }, new[] { tempSourceFile }, outputDir);

                // Assert - Check the generated file
                var outputFile = Path.Combine(outputDir, "TestClass.cs");
                Assert.True(File.Exists(outputFile), $"Output file should exist: {outputFile}");
                
                var generatedContent = File.ReadAllText(outputFile);
                Assert.NotNull(generatedContent);
                
                // Should contain prefix comments before parameter names
                Assert.Contains("/* IN */ const CString& param1", generatedContent);
                Assert.Contains("/* OUT */ CAgrMT* param2", generatedContent);
            }
            finally
            {
                File.Delete(tempSourceFile);
                File.Delete(tempHeaderFile);
            }
        }

        [Fact]
        public void GenerateMethodSignature_WithSuffixComments_ShouldPositionCorrectly()
        {
            // Arrange - Method with suffix parameter comments
            var sourceContent = @"
#include ""TestClass.h""

void TestClass::TestMethod(const CString& param1 /* IN */, CAgrMT* param2 /* OUT */)
{
    // Implementation
}";

            var headerContent = @"
class TestClass
{
public:
    void TestMethod(const CString& param1, CAgrMT* param2);
};";

            var tempSourceFile = Path.GetTempFileName();
            var tempHeaderFile = Path.GetTempFileName();
            var outputDir = Path.GetTempPath();

            try
            {
                File.WriteAllText(tempSourceFile, sourceContent);
                File.WriteAllText(tempHeaderFile, headerContent);

                // Act
                var converter = new CppToCsStructuralConverter();
                converter.ConvertFiles(new[] { tempHeaderFile }, new[] { tempSourceFile }, outputDir);

                // Assert - Check the generated file
                var outputFile = Path.Combine(outputDir, "TestClass.cs");
                Assert.True(File.Exists(outputFile), $"Output file should exist: {outputFile}");
                
                var generatedContent = File.ReadAllText(outputFile);
                Assert.NotNull(generatedContent);
                
                // Should contain suffix comments after parameter names
                Assert.Contains("const CString& param1 /* IN */", generatedContent);
                Assert.Contains("CAgrMT* param2 /* OUT */", generatedContent);
            }
            finally
            {
                File.Delete(tempSourceFile);
                File.Delete(tempHeaderFile);
            }
        }

        [Fact]
        public void GenerateMethodSignature_WithMixedComments_ShouldPositionCorrectly()
        {
            // Arrange - Method with mixed parameter comment positions
            var sourceContent = @"
#include ""TestClass.h""

void TestClass::TestMethod(
    /* IN */ const CString& param1 /* NOT_NULL */,
    const bool& param2 /* IN */,
    /* OUT */ CAgrMT* param3
)
{
    // Implementation
}";

            var headerContent = @"
class TestClass
{
public:
    void TestMethod(const CString& param1, const bool& param2, CAgrMT* param3);
};";

            var tempSourceFile = Path.GetTempFileName();
            var tempHeaderFile = Path.GetTempFileName();
            var outputDir = Path.GetTempPath();

            try
            {
                File.WriteAllText(tempSourceFile, sourceContent);
                File.WriteAllText(tempHeaderFile, headerContent);

                // Act
                var converter = new CppToCsStructuralConverter();
                converter.ConvertFiles(new[] { tempHeaderFile }, new[] { tempSourceFile }, outputDir);

                // Assert - Check the generated file
                var outputFile = Path.Combine(outputDir, "TestClass.cs");
                Assert.True(File.Exists(outputFile), $"Output file should exist: {outputFile}");
                
                var generatedContent = File.ReadAllText(outputFile);
                Assert.NotNull(generatedContent);
                
                // Param1: Both prefix and suffix
                Assert.Contains("/* IN */ const CString& param1 /* NOT_NULL */", generatedContent);
                
                // Param2: Suffix only
                Assert.Contains("const bool& param2 /* IN */", generatedContent);
                
                // Param3: Prefix only
                Assert.Contains("/* OUT */ CAgrMT* param3", generatedContent);
            }
            finally
            {
                File.Delete(tempSourceFile);
                File.Delete(tempHeaderFile);
            }
        }

        [Fact]
        public void BackwardCompatibility_LegacyInlineComments_ShouldStillWork()
        {
            // Arrange - Test that legacy InlineComments are still populated
            var headerContent = @"
class TestClass
{
public:
    void TestMethod(/* IN */ const CString& param1 /* OUT */);
};";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, headerContent);

            try
            {
                // Act
                var parser = new CppHeaderParser();
                var classes = parser.ParseHeaderFile(tempFile);

                // Assert
                Assert.Single(classes);
                var method = classes[0].Methods[0];
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
    }
}