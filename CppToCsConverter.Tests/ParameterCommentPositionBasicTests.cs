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
    }
}