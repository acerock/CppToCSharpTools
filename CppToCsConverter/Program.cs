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
                Console.WriteLine("Usage: CppToCsConverter <source_directory> [output_directory]");
                Console.WriteLine("Example: CppToCsConverter C:\\Source\\CppProject C:\\Output\\CsProject");
                return;
            }

            string sourceDirectory = args[0];
            string outputDirectory = args.Length > 1 ? args[1] : Path.Combine(sourceDirectory, "Generated_CS");

            if (!Directory.Exists(sourceDirectory))
            {
                Console.WriteLine($"Error: Source directory '{sourceDirectory}' does not exist.");
                return;
            }

            try
            {
                var converter = new CppToCsStructuralConverter();
                converter.ConvertDirectory(sourceDirectory, outputDirectory);
                
                Console.WriteLine($"Conversion completed successfully!");
                Console.WriteLine($"Output directory: {outputDirectory}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during conversion: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}