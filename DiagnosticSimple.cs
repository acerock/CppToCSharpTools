using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CppToCsConverter.Core.Parsers;
using CppToCsConverter.Core.Generators;
using CppToCsConverter.Core.Models;

class DiagnosticSimple
{
    static void Main()
    {
        // Test with CSample header content from the failing tests
        string headerContent = @"
class CSample
{
public:
    int value;
    string name;
    void DoSomething();
private:
    double internal_value;
};";

        Console.WriteLine("=== Header Content ===");
        Console.WriteLine(headerContent);
        Console.WriteLine();

        // Create a temporary file to parse
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, headerContent);
        
        try
        {
            // Parse header
            var parser = new CppHeaderParser();
            var classes = parser.ParseHeaderFile(tempFile);

            Console.WriteLine($"=== Parsed Classes: {classes.Count} ===");
            foreach (var cls in classes)
            {
                Console.WriteLine($"Class: {cls.Name}");
                Console.WriteLine($"Members: {cls.Members.Count}");
                foreach (var member in cls.Members)
                {
                    Console.WriteLine($"  - {member.Type} {member.Name} (Access: {member.AccessSpecifier})");
                }
                Console.WriteLine($"Methods: {cls.Methods.Count}");
                foreach (var method in cls.Methods)
                {
                    Console.WriteLine($"  - {method.ReturnType} {method.Name}({string.Join(", ", method.Parameters.Select(p => p.Type + " " + p.Name))}) (Access: {method.AccessSpecifier})");
                }
            }
            Console.WriteLine();

            // Generate C# with empty implementation methods (as tests do)
            var generator = new CsClassGenerator();
            var csClass = generator.GenerateClass(classes[0], new List<CppMethod>(), "TestFile.h");

            Console.WriteLine("=== Generated C# Class ===");
            Console.WriteLine(csClass);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}