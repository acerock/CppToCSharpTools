using System;
using System.IO;

class Program
{
    static void Main()
    {
        var content = File.ReadAllText(@"Work\SampleNet\CSample.cs");
        var bytes = File.ReadAllBytes(@"Work\SampleNet\CSample.cs");
        
        // Look for the method implementation area
        int methodStart = content.IndexOf("if (dimPd.IsEmpty())");
        if (methodStart == -1)
        {
            Console.WriteLine("Method not found");
            return;
        }
        
        // Check 50 characters before and after
        int start = Math.Max(0, methodStart - 50);
        int end = Math.Min(bytes.Length, methodStart + 100);
        
        Console.WriteLine($"Checking bytes {start} to {end}:");
        for (int i = start; i < end; i++)
        {
            byte b = bytes[i];
            if (b == 13) // \r
                Console.Write("\\r");
            else if (b == 10) // \n
                Console.Write("\\n");
            else if (b >= 32 && b <= 126)
                Console.Write((char)b);
            else
                Console.Write($"[{b:X2}]");
        }
        Console.WriteLine();
        
        // Count line ending types
        string text = File.ReadAllText(@"Work\SampleNet\CSample.cs");
        int crlfCount = 0;
        int lfOnlyCount = 0;
        
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                if (i > 0 && text[i - 1] == '\r')
                    crlfCount++;
                else
                    lfOnlyCount++;
            }
        }
        
        Console.WriteLine($"CRLF count: {crlfCount}");
        Console.WriteLine($"LF-only count: {lfOnlyCount}");
    }
}