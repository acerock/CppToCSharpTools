using System;
using System.Text.RegularExpressions;
using Xunit;

namespace CppToCsConverter.Tests
{
    public class SpecificRegexDebugTest
    {
        private readonly Regex _memberRegex = new Regex(@"^\s*(?:(static)\s+)?(?:(const)\s+)?(\w+(?:\s*\*|\s*&)?)\s+(\w+)(?:\s*\[\s*(\d*)\s*\])?(?:\s*=\s*([^;]+))?;\s*(.*)$", RegexOptions.Compiled);

        [Fact]
        public void Test_SpecificProblematicLines()
        {
            var testLines = new[]
            {
                "CAgrMT* m_pmtReport0; //Res/Rate-Reporting To do: Not touch",
                "CAgrMT* m_pmtReport1; //Res/Rate-Reporting (To do: Not touch",
                "CAgrMT* m_pmtReport2; //Res/Rate-Reporting (To do: Not touch",
                "CAgrMT* m_pmtReport3; //Res/Rate-Reporting (To do: Not touch)",
                "CAgrMT* m_pmtReport4; //Res/Rate-Reporting To do: Not touch?",
                "CAgrMT* m_pmtReport5; //Res/Rate-Reporting (To do: Not touch?)"
            };

            foreach (var line in testLines)
            {
                var match = _memberRegex.Match(line);
                Console.WriteLine($"Line: '{line}'");
                Console.WriteLine($"  Matches: {match.Success}");
                if (match.Success)
                {
                    Console.WriteLine($"  Groups[3] (Type): '{match.Groups[3].Value}'");
                    Console.WriteLine($"  Groups[4] (Name): '{match.Groups[4].Value}'");
                    Console.WriteLine($"  Groups[7] (Comment): '{match.Groups[7].Value}'");
                }
                Console.WriteLine();
            }
        }
    }
}