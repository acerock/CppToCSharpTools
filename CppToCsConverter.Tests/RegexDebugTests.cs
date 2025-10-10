using Xunit;
using System.Text.RegularExpressions;

namespace CppToCsConverter.Tests
{
    /// <summary>
    /// Debug tests for regex pattern matching
    /// </summary>
    public class RegexDebugTests
    {
        [Fact]
        public void Debug_RegexPatternMatching()
        {
            // Updated regex pattern that captures postfix comments
            var regex = new Regex(@"^\s*(?:(static)\s+)?(?:(const)\s+)?(\w+(?:\s*\*|\s*&)?)\s+(\w+)(?:\s*\[\s*(\d*)\s*\])?(?:\s*=\s*([^;]+))?;\s*(.*)$", RegexOptions.Compiled);
            
            // Test lines
            var testLines = new[]
            {
                "    agrint m_value1;", // This should match
                "    CString cValue1;", // This should match
                "    CAgrMT* m_pmtReport; //Res/Rate-Reporting (To do: Not touch?)", // This should match but might not
                "    static agrint m_iIndex;" // This should match
            };

            foreach (var line in testLines)
            {
                var match = regex.Match(line);
                System.Console.WriteLine($"Line: '{line}'");
                System.Console.WriteLine($"  Matches: {match.Success}");
                if (match.Success)
                {
                    System.Console.WriteLine($"  Groups count: {match.Groups.Count}");
                    for (int i = 0; i < match.Groups.Count; i++)
                    {
                        System.Console.WriteLine($"    Group[{i}]: '{match.Groups[i].Value}'");
                    }
                }
                System.Console.WriteLine();
            }
        }
    }
}