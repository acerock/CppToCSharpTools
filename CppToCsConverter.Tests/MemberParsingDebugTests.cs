using Xunit;
using System.IO;
using CppToCsConverter.Core.Parsers;
using System.Linq;

namespace CppToCsConverter.Tests
{
    /// <summary>
    /// Debug tests for member variable parsing
    /// </summary>
    public class MemberParsingDebugTests
    {
        private readonly CppHeaderParser _headerParser;

        public MemberParsingDebugTests()
        {
            _headerParser = new CppHeaderParser();
        }

        [Fact]
        public void Debug_ParseSimpleMemberVariable()
        {
            // Arrange - Very simple member variable
            var headerContent = @"
class CSample
{
private:
    int m_value;
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
                
                // Debug output
                System.Console.WriteLine($"Class Name: {cppClass.Name}");
                System.Console.WriteLine($"Members Count: {cppClass.Members.Count}");
                
                foreach (var member in cppClass.Members)
                {
                    System.Console.WriteLine($"Member: {member.Type} {member.Name}");
                }

                Assert.Single(cppClass.Members);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void Debug_ParseMemberWithPostfixComment()
        {
            // Arrange - Member with postfix comment
            var headerContent = @"
class CSample
{
private:
    int m_value; // Comment here
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
                
                // Debug output
                System.Console.WriteLine($"Class Name: {cppClass.Name}");
                System.Console.WriteLine($"Members Count: {cppClass.Members.Count}");
                
                foreach (var member in cppClass.Members)
                {
                    System.Console.WriteLine($"Member: {member.Type} {member.Name}, PostfixComment: '{member.PostfixComment}'");
                }

                Assert.Single(cppClass.Members);
                if (cppClass.Members.Count > 0)
                {
                    Assert.Equal("// Comment here", cppClass.Members[0].PostfixComment);
                }
            }
            finally
            {
                File.Delete(tempFile);
            }
        }
    }
}