using System;
using System.IO;
using System.Linq;
using CppToCsConverter.Core.Generators;
using CppToCsConverter.Core.Models;
using CppToCsConverter.Core.Parsers;

namespace CppToCsConverter.Tests
{
    public class DebugOutputTest
    {
        public static void TestClassGeneration()
        {
            var generator = new CsClassGenerator();
            var headerParser = new CppHeaderParser();
            
            var headerContent = @"
class CSample
{
private:
    agrint m_value1;
    CString cValue1;

public:
    CSample();
    void PublicMethod();

private:
    bool PrivateMethod();
};";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, headerContent);

            try
            {
                var classes = headerParser.ParseHeaderFile(tempFile);
                var cppClass = classes.FirstOrDefault(c => c.Name == "CSample");
                
                Console.WriteLine($"Parsed class: {cppClass?.Name}");
                Console.WriteLine($"Members count: {cppClass?.Members?.Count}");
                Console.WriteLine($"Methods count: {cppClass?.Methods?.Count}");
                
                if (cppClass != null)
                {
                    var result = generator.GenerateClass(cppClass, new System.Collections.Generic.List<CppMethod>(), "CSample");
                    Console.WriteLine("Generated C# code:");
                    Console.WriteLine("===================");
                    Console.WriteLine(result);
                    Console.WriteLine("===================");
                }
            }
            finally
            {
                File.Delete(tempFile);
            }
        }
        
        public static void TestTypeConversion()
        {
            var converter = new TypeConverter();
            
            string[] testTypes = { "std::string", "std::vector<int>", "DWORD", "LPSTR", "string", "int" };
            
            Console.WriteLine("Type Conversion Results:");
            foreach (var type in testTypes)
            {
                var result = converter.ConvertType(type);
                Console.WriteLine($"{type} -> {result}");
            }
        }
    }
}