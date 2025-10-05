using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using CppToCsConverter.Core;

namespace CppToCsConverter
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("C++ to C# Structural Converter");
            Console.WriteLine("==============================");

            if (args.Length < 1)
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("  CppToCsConverter <source_directory> [output_directory]");
                Console.WriteLine("  CppToCsConverter <source_directory> <file1,file2,...> [output_directory]");
                Console.WriteLine();
                Console.WriteLine("Examples:");
                Console.WriteLine("  CppToCsConverter C:\\Source\\CppProject");
                Console.WriteLine("  CppToCsConverter C:\\Source\\CppProject C:\\Output\\CsProject");
                Console.WriteLine("  CppToCsConverter C:\\Source\\CppProject filea.h,filea.cpp,fileb.cpp");
                Console.WriteLine("  CppToCsConverter C:\\Source\\CppProject filea.h,filea.cpp,fileb.cpp C:\\Output\\CsProject");
                return;
            }

            string sourceDirectory = args[0];
            string[]? specificFiles = null;
            string outputDirectory;

            // Parse arguments based on their structure
            if (args.Length == 2)
            {
                // Could be: <source> <output> OR <source> <files>
                if (args[1].Contains(",") || args[1].EndsWith(".h") || args[1].EndsWith(".cpp"))
                {
                    // It's a file list
                    specificFiles = args[1].Split(',', StringSplitOptions.RemoveEmptyEntries)
                                          .Select(f => f.Trim())
                                          .ToArray();
                    outputDirectory = Path.Combine(sourceDirectory, "Generated_CS");
                }
                else
                {
                    // It's an output directory
                    outputDirectory = args[1];
                }
            }
            else if (args.Length == 3)
            {
                // <source> <files> <output>
                specificFiles = args[1].Split(',', StringSplitOptions.RemoveEmptyEntries)
                                      .Select(f => f.Trim())
                                      .ToArray();
                outputDirectory = args[2];
            }
            else
            {
                // Default: <source>
                outputDirectory = Path.Combine(sourceDirectory, "Generated_CS");
            }

            if (!Directory.Exists(sourceDirectory))
            {
                Console.WriteLine($"Error: Source directory '{sourceDirectory}' does not exist.");
                return;
            }

            try
            {
                var converter = new CppToCsConverterApi();
                
                if (specificFiles != null && specificFiles.Length > 0)
                {
                    converter.ConvertSpecificFiles(sourceDirectory, specificFiles, outputDirectory);
                }
                else
                {
                    converter.ConvertDirectory(sourceDirectory, outputDirectory);
                }
                
                Console.WriteLine($"Conversion completed successfully!");
                Console.WriteLine($"Output directory: {outputDirectory}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during conversion: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}