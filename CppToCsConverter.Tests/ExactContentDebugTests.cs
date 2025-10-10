using Xunit;
using System.IO;
using CppToCsConverter.Core.Parsers;
using System.Linq;

namespace CppToCsConverter.Tests
{
    /// <summary>
    /// Debug tests - testing exact problematic content
    /// </summary>
    public class ExactContentDebugTests
    {
        private readonly CppHeaderParser _headerParser;

        public ExactContentDebugTests()
        {
            _headerParser = new CppHeaderParser();
        }

        [Fact]
        public void Debug_ExactProblematicLine()
        {
            // Exact problematic line in isolation
            var headerContent = @"
class CSample : public ISample
{
private:
    CAgrMT* m_pmtReport; //Res/Rate-Reporting (To do: Not touch?)
};";
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, headerContent);

            try
            {
                var classes = _headerParser.ParseHeaderFile(tempFile);
                System.Console.WriteLine($"Exact problematic: Classes={classes.Count}, Members={classes[0]?.Members.Count ?? 0}");
                if (classes.Count > 0 && classes[0].Members.Count > 0)
                {
                    var member = classes[0].Members[0];
                    System.Console.WriteLine($"  Member: {member.Type} {member.Name}, PostfixComment: '{member.PostfixComment}'");
                }
            }
            finally { File.Delete(tempFile); }
        }

        [Fact]
        public void Debug_TwoMembers()
        {
            // Two members, but simple
            var headerContent = @"
class CSample : public ISample
{
private:
    int member1;
    int member2;
};";
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, headerContent);

            try
            {
                var classes = _headerParser.ParseHeaderFile(tempFile);
                System.Console.WriteLine($"Two simple: Classes={classes.Count}, Members={classes[0]?.Members.Count ?? 0}");
            }
            finally { File.Delete(tempFile); }
        }
        
        [Fact]
        public void Debug_TwoMembersOneWithComment()
        {
            // Two members, one with comment
            var headerContent = @"
class CSample : public ISample
{
private:
    int member1; // comment
    int member2;
};";
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, headerContent);

            try
            {
                var classes = _headerParser.ParseHeaderFile(tempFile);
                System.Console.WriteLine($"Two with comment: Classes={classes.Count}, Members={classes[0]?.Members.Count ?? 0}");
            }
            finally { File.Delete(tempFile); }
        }
    }
}