using Xunit;
using System.IO;
using CppToCsConverter.Core.Parsers;
using System.Linq;
using CppToCsConverter.Core.Generators;
using System.Text;

namespace CppToCsConverter.Tests
{
    /// <summary>
    /// Tests for member variable postfix comments based on readme.md requirements.
    /// Covers single-line and multi-line postfix comments on member variable declarations.
    /// </summary>
    public class MemberPostfixCommentTests
    {
        private readonly CppHeaderParser _headerParser;
        private readonly CsClassGenerator _generator;

        public MemberPostfixCommentTests()
        {
            _headerParser = new CppHeaderParser();
            _generator = new CsClassGenerator();
        }

        [Fact]
        public void ParseMemberVariable_WithSingleLinePostfixComment_ShouldCaptureComment()
        {
            // Arrange - Based on readme.md example: CAgrMT* m_pmtReport; //Res/Rate-Reporting (To do: Not touch?)
            var headerContent = @"
class CSample : public ISample
{
private:
    CAgrMT* m_pmtReport; //Res/Rate-Reporting (To do: Not touch?)
    agrint m_value1;
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
                
                var memberWithComment = cppClass.Members.FirstOrDefault(m => m.Name == "m_pmtReport");
                Assert.NotNull(memberWithComment);
                Assert.Equal("//Res/Rate-Reporting (To do: Not touch?)", memberWithComment.PostfixComment);

                var memberWithoutComment = cppClass.Members.FirstOrDefault(m => m.Name == "m_value1");
                Assert.NotNull(memberWithoutComment);
                Assert.Empty(memberWithoutComment.PostfixComment);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseMemberVariable_WithMultiLinePostfixComment_SameLine_ShouldCaptureComment()
        {
            // Arrange - Multi-line comment that starts and ends on the same line
            var headerContent = @"
class CSample : public ISample
{
private:
    agrint m_value; /* This is a comment for the m_value member */
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
                Assert.Single(cppClass.Members);
                
                var member = cppClass.Members[0];
                Assert.Equal("m_value", member.Name);
                Assert.Equal("/* This is a comment for the m_value member */", member.PostfixComment);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseMemberVariable_WithMultiLinePostfixComment_SpanningLines_ShouldCaptureFullComment()
        {
            // Arrange - Multi-line comment spanning multiple lines as per readme.md example
            var headerContent = @"
class CSample : public ISample
{
private:
    agrint m_value; /* This is a comment for the m_value member and
                     * it might span multiple lines 
                     */
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
                Assert.Single(cppClass.Members);
                
                var member = cppClass.Members[0];
                Assert.Equal("m_value", member.Name);
                Assert.Contains("This is a comment for the m_value member and", member.PostfixComment);
                Assert.Contains("it might span multiple lines", member.PostfixComment);
                Assert.StartsWith("/* This is a comment", member.PostfixComment);
                Assert.EndsWith("*/", member.PostfixComment);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseMemberVariable_WithBothPrefixAndPostfixComments_ShouldCaptureBoth()
        {
            // Arrange - Member variable with both preceding and postfix comments
            var headerContent = @"
class CSample : public ISample
{
private:
    // My value holder
    agrint m_value; // Additional info
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
                Assert.Single(cppClass.Members);
                
                var member = cppClass.Members[0];
                Assert.Equal("m_value", member.Name);
                Assert.Single(member.PrecedingComments);
                Assert.Contains("// My value holder", member.PrecedingComments[0]);
                Assert.Equal("// Additional info", member.PostfixComment);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseMemberVariable_WithNoPostfixComment_ShouldHaveEmptyPostfixComment()
        {
            // Arrange - Member variable with no postfix comment
            var headerContent = @"
class CSample : public ISample
{
private:
    agrint m_value;
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
                Assert.Single(cppClass.Members);
                
                var member = cppClass.Members[0];
                Assert.Equal("m_value", member.Name);
                Assert.Empty(member.PostfixComment);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void GenerateCsClass_WithMemberPostfixComments_ShouldIncludeInOutput()
        {
            // Arrange - Based on readme.md example
            var headerContent = @"
class CSample : public ISample
{
private:
    // My value holder
    agrint m_value; /* This is a comment for the m_value member and
                     * it might span multiple lines 
                     */
    CAgrMT* m_pmtReport; //Res/Rate-Reporting (To do: Not touch?)
};";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, headerContent);

            try
            {
                // Act
                var classes = _headerParser.ParseHeaderFile(tempFile);
                var cppClass = classes[0];
                var result = _generator.GenerateClass(cppClass, new System.Collections.Generic.List<CppToCsConverter.Core.Models.CppMethod>(), "CSample");

                // Assert
                Assert.Contains("// My value holder", result);
                Assert.Contains("private agrint m_value; /* This is a comment for the m_value member and", result);
                Assert.Contains("private CAgrMT* m_pmtReport; //Res/Rate-Reporting (To do: Not touch?)", result);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseMemberVariable_StaticWithPostfixComment_ShouldCaptureComment()
        {
            // Arrange - Static member with postfix comment
            var headerContent = @"
class CSample : public ISample
{
private:
    static agrint s_staticValue; // Static member comment
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
                Assert.Single(cppClass.Members);
                
                var member = cppClass.Members[0];
                Assert.Equal("s_staticValue", member.Name);
                Assert.True(member.IsStatic);
                Assert.Equal("// Static member comment", member.PostfixComment);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseMemberVariable_ArrayWithPostfixComment_ShouldCaptureComment()
        {
            // Arrange - Array member with postfix comment
            var headerContent = @"
class CSample : public ISample
{
public:
    static const CString ColFrom[4]; // Array of column names
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
                Assert.Single(cppClass.Members);
                
                var member = cppClass.Members[0];
                Assert.Equal("ColFrom", member.Name);
                Assert.True(member.IsArray);
                Assert.Equal("4", member.ArraySize);
                Assert.Equal("// Array of column names", member.PostfixComment);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }
    }
}