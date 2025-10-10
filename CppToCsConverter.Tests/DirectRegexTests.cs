using Xunit;
using System.Text.RegularExpressions;

namespace CppToCsConverter.Tests
{
    /// <summary>
    /// Direct regex testing to isolate the issue
    /// </summary>
    public class DirectRegexTests
    {
        [Fact]
        public void Debug_DirectRegexTesting()
        {
            // Current regex pattern
            var regex = new Regex(@"^\s*(?:(static)\s+)?(?:(const)\s+)?(\w+(?:\s*\*|\s*&)?)\s+(\w+)(?:\s*\[\s*(\d*)\s*\])?(?:\s*=\s*([^;]+))?;\s*(.*)$", RegexOptions.Compiled);
            
            var testLines = new[]
            {
                "    CAgrMT* m_pmtReport; //Res/Rate-Reporting (To do: Not touch?)",  // Fails
                "    CAgrMT* m_pmtReport3; //Res/Rate-Reporting (To do: Not touch)",   // Works  
                "    CAgrMT* test1; //comment?",                                       // Works
                "    CAgrMT* test2; //comment()",                                     // Fails
                "    CAgrMT* test3; //comment(?)"                                     // Fails
            };

            foreach (var line in testLines)
            {
                var match = regex.Match(line);
                System.Console.WriteLine($"Line: '{line}'");
                System.Console.WriteLine($"  Matches: {match.Success}");
                if (match.Success)
                {
                    System.Console.WriteLine($"  Groups count: {match.Groups.Count}");
                    for (int i = 0; i < match.Groups.Count && i < 8; i++)
                    {
                        System.Console.WriteLine($"    Group[{i}]: '{match.Groups[i].Value}'");
                    }
                }
                else
                {
                    System.Console.WriteLine("  ** REGEX DOES NOT MATCH **");
                }
                System.Console.WriteLine();
            }
        }

        [Fact]
        public void Debug_SimplifiedRegex()
        {
            // Let's try a simpler regex to isolate the issue
            var simpleRegex = new Regex(@"^\s*(\w+\*?)\s+(\w+)\s*;\s*(.*)$", RegexOptions.Compiled);
            
            var testLines = new[]
            {
                "    CAgrMT* m_pmtReport; //Res/Rate-Reporting (To do: Not touch?)",
                "    CAgrMT* test2; //comment()"
            };

            System.Console.WriteLine("=== SIMPLE REGEX TEST ===");
            foreach (var line in testLines)
            {
                var match = simpleRegex.Match(line);
                System.Console.WriteLine($"Line: '{line}'");
                System.Console.WriteLine($"  Simple Matches: {match.Success}");
                if (match.Success)
                {
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