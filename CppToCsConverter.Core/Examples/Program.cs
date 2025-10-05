using CppToCsConverter.Core;

namespace ExampleUsage
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("CppToCsConverter.Core Example Usage");
            Console.WriteLine("===================================");

            // Example 1: Convert all C++ files in a directory
            Example1_ConvertDirectory();

            // Example 2: Convert specific files
            Example2_ConvertSpecificFiles();

            // Example 3: Convert files directly with full paths
            Example3_ConvertFilesDirect();

            Console.WriteLine("\nAll examples completed!");
        }

        static void Example1_ConvertDirectory()
        {
            Console.WriteLine("\nExample 1: Converting all files in directory");
            Console.WriteLine("--------------------------------------------");

            try
            {
                var converter = new CppToCsConverterApi();
                
                // This will find all .h and .cpp files in the source directory
                // and convert them to C# equivalents
                converter.ConvertDirectory(
                    sourceDirectory: @"C:\MyProject\Source",
                    outputDirectory: @"C:\MyProject\Generated"
                );

                Console.WriteLine("✓ Directory conversion completed successfully!");
            }
            catch (DirectoryNotFoundException)
            {
                Console.WriteLine("⚠ Directory not found (this is expected in the example)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error: {ex.Message}");
            }
        }

        static void Example2_ConvertSpecificFiles()
        {
            Console.WriteLine("\nExample 2: Converting specific files");
            Console.WriteLine("-----------------------------------");

            try
            {
                var converter = new CppToCsConverterApi();

                // Convert only specific files from the source directory
                converter.ConvertSpecificFiles(
                    sourceDirectory: @"C:\MyProject\Source",
                    specificFiles: new[] { "MyClass.h", "MyClass.cpp", "Helper.h" },
                    outputDirectory: @"C:\MyProject\Generated"
                );

                Console.WriteLine("✓ Specific files conversion completed successfully!");
            }
            catch (DirectoryNotFoundException)
            {
                Console.WriteLine("⚠ Directory not found (this is expected in the example)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error: {ex.Message}");
            }
        }

        static void Example3_ConvertFilesDirect()
        {
            Console.WriteLine("\nExample 3: Converting files with full paths");
            Console.WriteLine("------------------------------------------");

            try
            {
                var converter = new CppToCsConverterApi();

                // Convert files by specifying full paths
                converter.ConvertFiles(
                    headerFiles: new[] { 
                        @"C:\MyProject\Include\MyClass.h",
                        @"C:\MyProject\Include\Helper.h"
                    },
                    sourceFiles: new[] { 
                        @"C:\MyProject\Src\MyClass.cpp"
                    },
                    outputDirectory: @"C:\MyProject\Generated"
                );

                Console.WriteLine("✓ Direct file conversion completed successfully!");
            }
            catch (DirectoryNotFoundException)
            {
                Console.WriteLine("⚠ Files not found (this is expected in the example)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error: {ex.Message}");
            }
        }
    }
}