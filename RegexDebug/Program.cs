using System;
using System.Text.RegularExpressions;

var methodRegex = new Regex(@"(?:(virtual)\s+)?(?:(static)\s+)?(?:(\w+(?:\s*\*|\s*&)?(?:::\w+)?)\s+)?([~]?\w+)\s*\(.*?\)(?:\s*(const))?(?:\s*:\s*([^{]*))?(?:\s*=\s*0)?(?:\s*\{.*?\})?", RegexOptions.Compiled | RegexOptions.Singleline);
var memberRegex = new Regex(@"^\s*(?:(static)\s+)?(?:(const)\s+)?(\w+(?:\s*\*|\s*&)?)\s+(\w+)(?:\s*\[\s*(\d*)\s*\])?(?:\s*=\s*([^;]+))?;\s*(.*)$", RegexOptions.Compiled);

var testLine = "    CAgrMT* m_pmtReport; //Res/Rate-Reporting (To do: Not touch?)";

Console.WriteLine($"Test line: '{testLine}'");
Console.WriteLine();

var methodMatch = methodRegex.Match(testLine);
Console.WriteLine($"Method regex matches: {methodMatch.Success}");
if (methodMatch.Success)
{
    Console.WriteLine("Method groups:");
    for (int i = 0; i < methodMatch.Groups.Count; i++)
    {
        Console.WriteLine($"  Group[{i}]: '{methodMatch.Groups[i].Value}'");
    }
}

Console.WriteLine();

var memberMatch = memberRegex.Match(testLine);
Console.WriteLine($"Member regex matches: {memberMatch.Success}");
if (memberMatch.Success)
{
    Console.WriteLine("Member groups:");
    for (int i = 0; i < memberMatch.Groups.Count; i++)
    {
        Console.WriteLine($"  Group[{i}]: '{memberMatch.Groups[i].Value}'");
    }
}
