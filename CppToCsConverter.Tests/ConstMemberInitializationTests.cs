using System;
using System.IO;
using System.Linq;
using Xunit;
using CppToCsConverter.Core.Parsers;
using CppToCsConverter.Core.Models;

namespace CppToCsConverter.Tests
{
    public class ConstMemberInitializationTests
    {
        [Fact]
        public void ConstMember_WithStringInitialization_ShouldParseCorrectly()
        {
            // Arrange
            var headerContent = @"
class CSample
{
private:
    const CString s_cStructFlatType = ""N"";
};";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, headerContent);

            try
            {
                // Act
                var parser = new CppHeaderParser();
                var result = parser.ParseHeaderFile(tempFile);

                // Assert
                Assert.Single(result);
                var cppClass = result[0];
                Assert.Equal("CSample", cppClass.Name);
                Assert.Single(cppClass.Members);
                
                var member = cppClass.Members[0];
                Assert.Equal("CString", member.Type);
                Assert.Equal("s_cStructFlatType", member.Name);
                Assert.True(member.IsConst);
                Assert.Equal("\"N\"", member.InitializationValue);
                Assert.Equal(AccessSpecifier.Private, member.AccessSpecifier);
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Fact]
        public void ConstMember_WithIntInitialization_ShouldParseCorrectly()
        {
            // Arrange
            var headerContent = @"
class CSample
{
public:
    const agrint gs_lStructLevelGLDimensionMin = 1;
};";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, headerContent);

            try
            {
                // Act
                var parser = new CppHeaderParser();
                var result = parser.ParseHeaderFile(tempFile);

                // Assert
                Assert.Single(result);
                var cppClass = result[0];
                Assert.Single(cppClass.Members);
                
                var member = cppClass.Members[0];
                Assert.Equal("agrint", member.Type);
                Assert.Equal("gs_lStructLevelGLDimensionMin", member.Name);
                Assert.True(member.IsConst);
                Assert.Equal("1", member.InitializationValue);
                Assert.Equal(AccessSpecifier.Public, member.AccessSpecifier);
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Fact]
        public void ConstMember_WithComments_ShouldPreserveComments()
        {
            // Arrange
            var headerContent = @"
class CSample
{
private:
    // Comment
    const CString s_cStructFlatType = ""N""; // More comment
};";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, headerContent);

            try
            {
                // Act
                var parser = new CppHeaderParser();
                var result = parser.ParseHeaderFile(tempFile);

                // Assert
                var member = result[0].Members[0];
                Assert.Single(member.PrecedingComments);
                Assert.Equal("// Comment", member.PrecedingComments[0]);
                Assert.Equal("// More comment", member.PostfixComment);
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Fact]
        public void ConstMember_GeneratesToCSharp_WithCorrectFormat()
        {
            // Arrange
            var headerContent = @"
class CSample
{
private:
    // Comment
    const CString s_cStructFlatType = ""N""; // More comment

public:
    const agrint gs_lStructLevelGLDimensionMin = 1;
};";

            var tempDir = Path.Combine(Path.GetTempPath(), "ConstMemberTest_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            var headerFile = Path.Combine(tempDir, "CSample.h");
            File.WriteAllText(headerFile, headerContent);

            try
            {
                // Act
                var converter = new CppToCsConverter.Core.Core.CppToCsStructuralConverter();
                converter.ConvertDirectory(tempDir, tempDir);

                // Assert
                var outputFile = Path.Combine(tempDir, "CSample.cs");
                Assert.True(File.Exists(outputFile));
                
                var generatedContent = File.ReadAllText(outputFile);
                
                // Should have const keyword with value
                Assert.Contains("private const CString s_cStructFlatType = \"N\";", generatedContent);
                Assert.Contains("public const agrint gs_lStructLevelGLDimensionMin = 1;", generatedContent);
                
                // Should preserve comments
                Assert.Contains("// Comment", generatedContent);
                Assert.Contains("// More comment", generatedContent);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }
    }
}
