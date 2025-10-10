using Xunit;
using System.IO;
using CppToCsConverter.Core.Parsers;
using System.Linq;

namespace CppToCsConverter.Tests
{
    /// <summary>
    /// Debug tests - testing the specific failing line
    /// </summary>
    public class MemberParsingDebugTests5
    {
        private readonly CppHeaderParser _headerParser;

        public MemberParsingDebugTests5()
        {
            _headerParser = new CppHeaderParser();
        }

        [Fact]
        public void Debug_SpecificFailingLine()
        {
            // Arrange - Just the specific line that's failing
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
        public void Debug_BothLines()
        {
            // Arrange - Both lines from original failing test
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