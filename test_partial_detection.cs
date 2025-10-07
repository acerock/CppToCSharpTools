using System;
using System.IO;
using System.Linq;
using CppToCsConverter.Core.Parsers;

class TestPartialDetection
{
    static void Main()
    {
        Console.WriteLine("Testing TargetFileName population...");
        
        // Parse header file
        var headerParser = new CppHeaderParser();
        var classes = headerParser.ParseHeaderFile("MultiFileClass.h");
        
        Console.WriteLine($"Found {classes.Count} classes");
        
        foreach (var cppClass in classes)
        {
            Console.WriteLine($"\nClass: {cppClass.Name}");
            Console.WriteLine($"Methods: {cppClass.Methods.Count}");
            
            foreach (var method in cppClass.Methods)
            {
                Console.WriteLine($"  Method: {method.Name}");
                Console.WriteLine($"    HasInlineImplementation: {method.HasInlineImplementation}");
                Console.WriteLine($"    TargetFileName: '{method.TargetFileName}'");
                Console.WriteLine($"    InlineImplementation: '{method.InlineImplementation}'");
            }
            
            Console.WriteLine($"\nIsPartialClass: {cppClass.IsPartialClass()}");
            Console.WriteLine($"TargetFileNames: [{string.Join(", ", cppClass.GetTargetFileNames())}]");
        }
        
        // Parse source files
        var sourceParser = new CppSourceParser();
        var (sourceMethods1, _) = sourceParser.ParseSourceFile("MultiFileClass_File1.cpp");
        var (sourceMethods2, _) = sourceParser.ParseSourceFile("MultiFileClass_File2.cpp");
        
        Console.WriteLine($"\nSource methods from File1: {sourceMethods1.Count}");
        foreach (var method in sourceMethods1)
        {
            Console.WriteLine($"  Method: {method.Name} - TargetFileName: '{method.TargetFileName}'");
        }
        
        Console.WriteLine($"\nSource methods from File2: {sourceMethods2.Count}");
        foreach (var method in sourceMethods2)
        {
            Console.WriteLine($"  Method: {method.Name} - TargetFileName: '{method.TargetFileName}'");
        }
    }
}