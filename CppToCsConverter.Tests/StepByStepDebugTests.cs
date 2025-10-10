using Xunit;
using System.IO;
using CppToCsConverter.Core.Parsers;
using System.Linq;

namespace CppToCsConverter.Tests
{
    /// <summary>
    /// Debug tests - step by step isolation
    /// </summary>
    public class StepByStepDebugTests
    {
        private readonly CppHeaderParser _headerParser;

        public StepByStepDebugTests()
        {
            _headerParser = new CppHeaderParser();
        }

        [Fact]
        public void Debug_JustInheritance()
        {
            // Just inheritance, no members
            var headerContent = @"
class CSample : public ISample
{
};";
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, headerContent);

            try
            {
                var classes = _headerParser.ParseHeaderFile(tempFile);
                System.Console.WriteLine($"Inheritance only: Classes={classes.Count}, Members={classes[0]?.Members.Count ?? 0}");
            }
            finally { File.Delete(tempFile); }
        }

        [Fact]
        public void Debug_InheritanceWithSimpleMember()
        {
            // Inheritance with simple member
            var headerContent = @"
class CSample : public ISample
{
private:
    int value;
};";
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, headerContent);

            try
            {
                var classes = _headerParser.ParseHeaderFile(tempFile);
                System.Console.WriteLine($"Simple member: Classes={classes.Count}, Members={classes[0]?.Members.Count ?? 0}");
            }
            finally { File.Delete(tempFile); }
        }

        [Fact]
        public void Debug_InheritanceWithCommentMember()
        {
            // Inheritance with commented member
            var headerContent = @"
class CSample : public ISample
{
private:
    int value; // comment here
};";
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, headerContent);

            try
            {
                var classes = _headerParser.ParseHeaderFile(tempFile);
                System.Console.WriteLine($"Commented member: Classes={classes.Count}, Members={classes[0]?.Members.Count ?? 0}");
            }
            finally { File.Delete(tempFile); }
        }
    }
}