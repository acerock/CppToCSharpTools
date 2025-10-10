using Xunit;
using System.IO;
using CppToCsConverter.Core.Parsers;
using System.Linq;

namespace CppToCsConverter.Tests
{
    /// <summary>
    /// Debug tests - exact replica of failing test with debug output
    /// </summary>
    public class MemberPostfixDebugTests
    {
        private readonly CppHeaderParser _headerParser;

        public MemberPostfixDebugTests()
        {
            _headerParser = new CppHeaderParser();
        }

        [Fact]
        public void Debug_ExactFailingTest()
        {
            // Arrange - EXACTLY the same as failing test
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

                // Debug output
                System.Console.WriteLine($"Classes Count: {classes.Count}");
                
                if (classes.Count > 0)
                {
                    var cppClass = classes[0];
                    System.Console.WriteLine($"Class Name: {cppClass.Name}");
                    System.Console.WriteLine($"Members Count: {cppClass.Members.Count}");
                    
                    for (int i = 0; i < cppClass.Members.Count; i++)
                    {
                        var member = cppClass.Members[i];
                        System.Console.WriteLine($"Member[{i}]: '{member.Type}' '{member.Name}', PostfixComment: '{member.PostfixComment}'");
                    }
                }
                
                // Assert
                Assert.Single(classes);
                var csampleClass = classes[0];
                System.Console.WriteLine($"Expected 2 members, got {csampleClass.Members.Count}");
                
                if (csampleClass.Members.Count > 0)
                {
                    var memberWithComment = csampleClass.Members.FirstOrDefault(m => m.Name == "m_pmtReport");
                    if (memberWithComment != null)
                    {
                        System.Console.WriteLine($"Found m_pmtReport with comment: '{memberWithComment.PostfixComment}'");
                    }
                    else
                    {
                        System.Console.WriteLine("m_pmtReport not found!");
                    }
                }
            }
            finally
            {
                File.Delete(tempFile);
            }
        }
    }
}