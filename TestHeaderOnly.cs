using System;
using System.IO;
using System.Linq;
using System.Text;
using CppToCsConverter.Core.Generators;
using CppToCsConverter.Core.Parsers;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== Header-Only Generation Test ===");
        
        var headerParser = new CppHeaderParser();
        var generator = new CsClassGenerator();
        
        var headerContent = @"
class CSample
{
private:
    agrint m_value1;
    CString cValue1;

public:
    CSample();
    void PublicMethod();
    bool InlineMethod() { return true; }

private:
    bool PrivateMethod();
};";

        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, headerContent);

        try
        {
            // Parse header
            var classes = headerParser.ParseHeaderFile(tempFile);
            var cppClass = classes.FirstOrDefault(c => c.Name == "CSample");

            Console.WriteLine($"Found classes: {classes.Count}");
            
            if (cppClass != null)
            {
                Console.WriteLine($"Class name: {cppClass.Name}");
                Console.WriteLine($"Members count: {cppClass.Members.Count}");
                Console.WriteLine($"Methods count: {cppClass.Methods.Count}");
                
                Console.WriteLine("\nMembers:");
                foreach (var member in cppClass.Members)
                {
                    Console.WriteLine($"  - {member.AccessSpecifier} {member.Type} {member.Name}");
                }
                
                Console.WriteLine("\nMethods:");
                foreach (var method in cppClass.Methods)
                {
                    Console.WriteLine($"  - {method.AccessSpecifier} {method.ReturnType} {method.Name}() [Inline: {method.HasInlineImplementation}]");
                }
                
                // Generate with empty implementation list (current failing scenario)
                var result = generator.GenerateClass(cppClass, new System.Collections.Generic.List<CppToCsConverter.Core.Models.CppMethod>(), "CSample");
                
                Console.WriteLine("\n=== Generated C# Code ===");
                Console.WriteLine(result);
                Console.WriteLine("=== End Generated Code ===");
                
                // Check what's missing
                Console.WriteLine("\n=== Checks ===");
                Console.WriteLine($"Contains members: {result.Contains("agrint m_value1")}");
                Console.WriteLine($"Contains methods: {result.Contains("void PublicMethod")}");
                Console.WriteLine($"Contains inline: {result.Contains("return true")}");
            }
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}