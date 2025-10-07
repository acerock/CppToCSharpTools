using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CppToCsConverter.Core.Parsers;
using CppToCsConverter.Core.Core;
using CppToCsConverter.Core.Models;

class Program
{
    static void Main()
    {
        Console.WriteLine("Testing TargetFileName population...");
        
        // Test with the failing static case
        var headerFile = "../PartialWithStatic.h";
        var source1File = "../PartialWithStatic.cpp";
        var source2File = "";
        
        if (!File.Exists(headerFile))
        {
            Console.WriteLine($"Header file not found: {headerFile}");
            return;
        }
        
        var headerParser = new CppHeaderParser();
        var sourceParser = new CppSourceParser();
        var converter = new CppToCsStructuralConverter();
        
        // Parse header file
        Console.WriteLine("\n=== PARSING HEADER ===");
        var classes = headerParser.ParseHeaderFile(headerFile);
        
        foreach (var cppClass in classes)
        {
            Console.WriteLine($"Class: {cppClass.Name}");
            Console.WriteLine($"Methods: {cppClass.Methods.Count}");
            
            foreach (var method in cppClass.Methods)
            {
                Console.WriteLine($"  Method: {method.Name}");
                Console.WriteLine($"    HasInlineImplementation: {method.HasInlineImplementation}");
                Console.WriteLine($"    TargetFileName: '{method.TargetFileName}'");
                Console.WriteLine($"    InlineImplementation: '{method.InlineImplementation?.Substring(0, Math.Min(50, method.InlineImplementation?.Length ?? 0)) ?? "null"}'");
            }
            
            Console.WriteLine($"\nIsPartialClass BEFORE enrichment: {cppClass.IsPartialClass()}");
            Console.WriteLine($"TargetFileNames: [{string.Join(", ", cppClass.GetTargetFileNames())}]");
        }
        
        // Parse source files
        if (File.Exists(source1File))
        {
            Console.WriteLine("\n=== PARSING SOURCE FILE 1 ===");
            var (sourceMethods1, _) = sourceParser.ParseSourceFile(source1File);
            
            Console.WriteLine($"Source methods from File1: {sourceMethods1.Count}");
            foreach (var method in sourceMethods1)
            {
                Console.WriteLine($"  Method: {method.ClassName}::{method.Name} - TargetFileName: '{method.TargetFileName}'");
            }
            
            // SIMULATE ENRICHMENT - match source methods with header methods
            if (classes.Any())
            {
                var cppClass = classes.First();
                foreach (var sourceMethod in sourceMethods1)
                {
                    var headerMethod = cppClass.Methods.FirstOrDefault(m => 
                        m.Name == sourceMethod.Name && ParametersMatch(m.Parameters, sourceMethod.Parameters));
                    
                    if (headerMethod != null)
                    {
                        headerMethod.TargetFileName = sourceMethod.TargetFileName;
                        headerMethod.ImplementationBody = sourceMethod.ImplementationBody;
                        Console.WriteLine($"  ✓ Enriched {headerMethod.Name} with TargetFileName: '{sourceMethod.TargetFileName}'");
                    }
                }
                
                Console.WriteLine($"\n=== AFTER ENRICHMENT ===");
                Console.WriteLine($"IsPartialClass AFTER enrichment: {cppClass.IsPartialClass()}");
                
                Console.WriteLine("Methods after enrichment:");
                foreach (var method in cppClass.Methods)
                {
                    Console.WriteLine($"  Method: {method.Name}");
                    Console.WriteLine($"    HasInlineImplementation: {method.HasInlineImplementation}");
                    Console.WriteLine($"    TargetFileName: '{method.TargetFileName}'");
                    Console.WriteLine($"    Has ImplementationBody: {!string.IsNullOrEmpty(method.ImplementationBody)}");
                }
            }
        }
        
        if (File.Exists(source2File))
        {
            Console.WriteLine("\n=== PARSING SOURCE FILE 2 ===");
            var (sourceMethods2, _) = sourceParser.ParseSourceFile(source2File);
            
            Console.WriteLine($"Source methods from File2: {sourceMethods2.Count}");
            foreach (var method in sourceMethods2)
            {
                Console.WriteLine($"  Method: {method.ClassName}::{method.Name} - TargetFileName: '{method.TargetFileName}'");
            }
        }
        
        // Test the converter
        Console.WriteLine("\n=== TESTING CONVERTER ===");
        var tempDir = Path.Combine(Path.GetTempPath(), "PartialClassTest");
        if (Directory.Exists(tempDir))
            Directory.Delete(tempDir, true);
            
        Directory.CreateDirectory(tempDir);
        
        // Copy test files
        if (File.Exists(headerFile))
            File.Copy(headerFile, Path.Combine(tempDir, "MultiFileClass.h"));
        if (File.Exists(source1File))
            File.Copy(source1File, Path.Combine(tempDir, "MultiFileClass_File1.cpp"));
        if (File.Exists(source2File))
            File.Copy(source2File, Path.Combine(tempDir, "MultiFileClass_File2.cpp"));
            
        try
        {
            converter.ConvertDirectory(tempDir, tempDir);
            
            var outputFile = Path.Combine(tempDir, "MultiFileClass.cs");
            if (File.Exists(outputFile))
            {
                var content = File.ReadAllText(outputFile);
                Console.WriteLine($"Generated file exists: {outputFile}");
                Console.WriteLine($"Contains 'partial': {content.Contains("partial")}");
                Console.WriteLine($"First 200 chars: {content.Substring(0, Math.Min(200, content.Length))}");
            }
            else
            {
                Console.WriteLine("No output file generated");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }
    
    private static bool ParametersMatch(List<CppParameter> headerParams, List<CppParameter> sourceParams)
    {
        if (headerParams.Count != sourceParams.Count) return false;
        
        for (int i = 0; i < headerParams.Count; i++)
        {
            if (headerParams[i].Type != sourceParams[i].Type ||
                headerParams[i].Name != sourceParams[i].Name)
                return false;
        }
        
        return true;
    }
}
