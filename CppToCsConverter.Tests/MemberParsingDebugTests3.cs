using Xunit;
using System.IO;
using CppToCsConverter.Core.Parsers;
using System.Linq;

namespace CppToCsConverter.Tests
{
    /// <summary>
    /// Debug tests for member variable parsing - testing inheritance vs no inheritance
    /// </summary>
    public class MemberParsingDebugTests3
    {
        private readonly CppHeaderParser _headerParser;

        public MemberParsingDebugTests3()
        {
            _headerParser = new CppHeaderParser();
        }

        [Fact]
        public void Debug_WithoutInheritance()
        {
            // Arrange - Same content but without inheritance
            var headerContent = @"
class CSample
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
                    System.Console.WriteLine($"IsInterface: {cppClass.IsInterface}");
                    System.Console.WriteLine($"Members Count: {cppClass.Members.Count}");
                    
                    foreach (var member in cppClass.Members)
                    {
                        System.Console.WriteLine($"Member: '{member.Type}' '{member.Name}', PostfixComment: '{member.PostfixComment}'");
                    }
                }
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void Debug_WithInheritance()
        {
            // Arrange - Same content with inheritance
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
                    System.Console.WriteLine($"IsInterface: {cppClass.IsInterface}");
                    System.Console.WriteLine($"Base Classes: {string.Join(", ", cppClass.BaseClasses)}");
                    System.Console.WriteLine($"Members Count: {cppClass.Members.Count}");
                    
                    foreach (var member in cppClass.Members)
                    {
                        System.Console.WriteLine($"Member: '{member.Type}' '{member.Name}', PostfixComment: '{member.PostfixComment}'");
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