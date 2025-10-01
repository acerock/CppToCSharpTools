using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CppToCsConverter.Models;
using CppToCsConverter.Parsers;
using CppToCsConverter.Generators;

namespace CppToCsConverter.Core
{
    public class CppToCsStructuralConverter
    {
        private readonly CppHeaderParser _headerParser;
        private readonly CppSourceParser _sourceParser;
        private readonly CsClassGenerator _classGenerator;
        private readonly CsInterfaceGenerator _interfaceGenerator;

        public CppToCsStructuralConverter()
        {
            _headerParser = new CppHeaderParser();
            _sourceParser = new CppSourceParser();
            _classGenerator = new CsClassGenerator();
            _interfaceGenerator = new CsInterfaceGenerator();
        }

        public void ConvertDirectory(string sourceDirectory, string outputDirectory)
        {
            Console.WriteLine($"Converting C++ files from: {sourceDirectory}");
            Console.WriteLine($"Output directory: {outputDirectory}");

            // Ensure output directory exists
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            // Find all .h and .cpp files
            var headerFiles = Directory.GetFiles(sourceDirectory, "*.h", SearchOption.AllDirectories);
            var sourceFiles = Directory.GetFiles(sourceDirectory, "*.cpp", SearchOption.AllDirectories);

            Console.WriteLine($"Found {headerFiles.Length} header files and {sourceFiles.Length} source files");

            // Parse all files
            var parsedHeaders = new Dictionary<string, CppClass>();
            var parsedSources = new Dictionary<string, List<CppMethod>>();

            // Parse header files
            foreach (var headerFile in headerFiles)
            {
                Console.WriteLine($"Parsing header: {Path.GetFileName(headerFile)}");
                var cppClass = _headerParser.ParseHeaderFile(headerFile);
                if (cppClass != null)
                {
                    parsedHeaders[cppClass.Name] = cppClass;
                }
            }

            // Parse source files
            foreach (var sourceFile in sourceFiles)
            {
                Console.WriteLine($"Parsing source: {Path.GetFileName(sourceFile)}");
                var methods = _sourceParser.ParseSourceFile(sourceFile);
                var fileName = Path.GetFileNameWithoutExtension(sourceFile);
                parsedSources[fileName] = methods;
            }

            // Generate C# files
            foreach (var kvp in parsedHeaders)
            {
                var className = kvp.Key;
                var cppClass = kvp.Value;

                Console.WriteLine($"Generating C# for class: {className}");

                if (cppClass.IsInterface)
                {
                    // Generate interface
                    var csInterface = _interfaceGenerator.GenerateInterface(cppClass);
                    var interfaceFileName = Path.Combine(outputDirectory, $"{className}.cs");
                    File.WriteAllText(interfaceFileName, csInterface);
                    Console.WriteLine($"Generated interface: {className}.cs");
                }
                else
                {
                    // Find all source files that contain methods for this class
                    var relatedSources = parsedSources
                        .Where(s => s.Value.Any(m => m.ClassName == className))
                        .ToList();

                    if (relatedSources.Count == 0)
                    {
                        // Class with only inline methods in header
                        var csClass = _classGenerator.GenerateClass(cppClass, new List<CppMethod>(), className);
                        var classFileName = Path.Combine(outputDirectory, $"{className}.cs");
                        File.WriteAllText(classFileName, csClass);
                        Console.WriteLine($"Generated class: {className}.cs");
                    }
                    else if (relatedSources.Count == 1)
                    {
                        // Single source file
                        var methods = relatedSources[0].Value.Where(m => m.ClassName == className).ToList();
                        var csClass = _classGenerator.GenerateClass(cppClass, methods, className);
                        var classFileName = Path.Combine(outputDirectory, $"{className}.cs");
                        File.WriteAllText(classFileName, csClass);
                        Console.WriteLine($"Generated class: {className}.cs");
                    }
                    else
                    {
                        // Multiple source files - generate partial classes
                        foreach (var sourceKvp in relatedSources)
                        {
                            var sourceFileName = sourceKvp.Key;
                            var methods = sourceKvp.Value.Where(m => m.ClassName == className).ToList();
                            
                            if (methods.Any())
                            {
                                var csClass = _classGenerator.GeneratePartialClass(cppClass, methods, sourceFileName);
                                var partialFileName = Path.Combine(outputDirectory, $"{sourceFileName}.cs");
                                File.WriteAllText(partialFileName, csClass);
                                Console.WriteLine($"Generated partial class: {sourceFileName}.cs");
                            }
                        }
                    }
                }
            }

            Console.WriteLine("Conversion completed!");
        }
    }
}