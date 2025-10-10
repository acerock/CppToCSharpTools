using Xunit;
using System.IO;
using CppToCsConverter.Core.Parsers;
using System.Linq;

namespace CppToCsConverter.Tests
{
    /// <summary>
    /// Debug tests - using exact same content as working test
    /// </summary>
    public class MemberParsingDebugTests4
    {
        private readonly CppHeaderParser _headerParser;

        public MemberParsingDebugTests4()
        {
            _headerParser = new CppHeaderParser();
        }

        [Fact]
        public void Debug_ExactWorkingTestContent()
        {
            // Arrange - Exact same content as working CommentAndRegionTests
            var headerContent = @"
class CSample : public ISample
{
private:

    // My value holder

    agrint m_value;
};";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, headerContent);

            try
            {
                // Act
                var classes = _headerParser.ParseHeaderFile(tempFile);

                // Assert and Debug
                System.Console.WriteLine($"Classes Count: {classes.Count}");
                
                if (classes.Count > 0)
                {
                    var cppClass = classes[0];
                    System.Console.WriteLine($"Class Name: {cppClass.Name}");
                    System.Console.WriteLine($"Members Count: {cppClass.Members.Count}");
                    
                    foreach (var member in cppClass.Members)
                    {
                        System.Console.WriteLine($"Member: '{member.Type}' '{member.Name}'");
                    }
                }
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void Debug_JustThePointerType()
        {
            // Arrange - Testing just the pointer type issue
            var headerContent = @"
class CSample : public ISample
{
private:
    CAgrMT* m_pmtReport;
};";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, headerContent);

            try
            {
                // Act
                var classes = _headerParser.ParseHeaderFile(tempFile);

                // Assert and Debug
                System.Console.WriteLine($"Classes Count: {classes.Count}");
                
                if (classes.Count > 0)
                {
                    var cppClass = classes[0];
                    System.Console.WriteLine($"Class Name: {cppClass.Name}");
                    System.Console.WriteLine($"Members Count: {cppClass.Members.Count}");
                    
                    foreach (var member in cppClass.Members)
                    {
                        System.Console.WriteLine($"Member: '{member.Type}' '{member.Name}'");
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