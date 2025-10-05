using System.IO;
using System.Linq;
using CppToCsConverter.Core.Parsers;
using Xunit;

namespace CppToCsConverter.Tests
{
    public class CommentBeforeMethodTests
    {
        [Fact]
        public void ParseHeaderFile_ShouldParseMethodWithCommentBefore()
        {
            // Arrange - Create a test header file with comment before method (like MethodP1 case)
            var headerContent = @"
class TestClass
{
private:
    // Comment from .h
    bool MethodWithComment(const TDimValue& dim1, const agrint& int1, const agrint& int2=0, bool bool1=false);
    
    bool MethodWithoutComment(const TDimValue& dim1, const agrint& int1, const agrint& int2=0);
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
                
                // Should have both methods
                Assert.Equal(2, testClass.Methods.Count);
                
                // Find the method with comment
                var methodWithComment = testClass.Methods.FirstOrDefault(m => m.Name == "MethodWithComment");
                Assert.NotNull(methodWithComment);
                
                // Should have the comment
                Assert.Single(methodWithComment.HeaderComments);
                Assert.Contains("// Comment from .h", methodWithComment.HeaderComments[0]);
                
                // Should have default parameters
                Assert.Equal(4, methodWithComment.Parameters.Count);
                Assert.Equal("0", methodWithComment.Parameters[2].DefaultValue);
                Assert.Equal("false", methodWithComment.Parameters[3].DefaultValue);
                
                // Find the method without comment for comparison
                var methodWithoutComment = testClass.Methods.FirstOrDefault(m => m.Name == "MethodWithoutComment");
                Assert.NotNull(methodWithoutComment);
                Assert.Empty(methodWithoutComment.HeaderComments);
                Assert.Equal(3, methodWithoutComment.Parameters.Count);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseHeaderFile_ShouldParseMethodAfterAccessSpecifierAndComment()
        {
            // Arrange - Reproduce the exact MethodP1 scenario
            var headerContent = @"
class CSample
{
public:
    void MethodTwo() { return cValue1 == cValue2; }

private:

    // Comment from .h
    bool MethodP1(const TDimValue& dim1, const agrint& int1, const agrint& int2=0, bool bool1=false);
    bool MethodP2(const TDimValue& dim1, const agrint& int1, const agrint& int2=0, bool bool1=false);
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
                var csampleClass = classes[0];
                
                // Should have all three methods (MethodTwo, MethodP1, MethodP2)
                Assert.Equal(3, csampleClass.Methods.Count);
                
                // Find MethodP1 specifically
                var methodP1 = csampleClass.Methods.FirstOrDefault(m => m.Name == "MethodP1");
                Assert.NotNull(methodP1);
                
                // Should have the comment
                Assert.Single(methodP1.HeaderComments);
                Assert.Contains("// Comment from .h", methodP1.HeaderComments[0]);
                
                // Should have 4 parameters with default values
                Assert.Equal(4, methodP1.Parameters.Count);
                Assert.Equal("0", methodP1.Parameters[2].DefaultValue);
                Assert.Equal("false", methodP1.Parameters[3].DefaultValue);
                
                // MethodP2 should also be parsed correctly for comparison
                var methodP2 = csampleClass.Methods.FirstOrDefault(m => m.Name == "MethodP2");
                Assert.NotNull(methodP2);
                Assert.Equal(4, methodP2.Parameters.Count);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }
    }
}