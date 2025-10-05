using System;
using System.IO;
using System.Linq;
using Xunit;
using CppToCsConverter.Core.Generators;
using CppToCsConverter.Core.Parsers;

namespace CppToCsConverter.Tests
{
    public class DeepDiagnosticTests
    {
        [Fact]
        public void TestHeaderParsingOnly_ShowWhatsParsed()
        {
            // Arrange
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
                // Act - Parse header only
                var classes = headerParser.ParseHeaderFile(tempFile);
                var cppClass = classes.FirstOrDefault(c => c.Name == "CSample");

                // Debug what was actually parsed
                Console.WriteLine($"=== HEADER PARSING RESULTS ===");
                Console.WriteLine($"Found classes: {classes.Count}");
                
                if (cppClass != null)
                {
                    Console.WriteLine($"Class name: {cppClass.Name}");
                    Console.WriteLine($"Members count: {cppClass.Members.Count}");
                    Console.WriteLine($"Methods count: {cppClass.Methods.Count}");
                    
                    Console.WriteLine("\nMembers found:");
                    foreach (var member in cppClass.Members)
                    {
                        Console.WriteLine($"  - {member.AccessSpecifier} {member.Type} {member.Name} (Static: {member.IsStatic})");
                    }
                    
                    Console.WriteLine("\nMethods found:");
                    foreach (var method in cppClass.Methods)
                    {
                        Console.WriteLine($"  - {method.AccessSpecifier} {method.ReturnType} {method.Name}() (Static: {method.IsStatic})");
                    }
                }

                // Now test generation with empty implementation list
                if (cppClass != null)
                {
                    var generator = new CsClassGenerator();
                    var result = generator.GenerateClass(cppClass, new System.Collections.Generic.List<CppToCsConverter.Core.Models.CppMethod>(), "CSample");
                    
                    Console.WriteLine($"\n=== GENERATED C# CODE ===");
                    Console.WriteLine(result);
                    Console.WriteLine($"=== END GENERATED CODE ===");
                    
                    // Check if specific strings exist
                    Console.WriteLine($"\n=== ASSERTION CHECKS ===");
                    Console.WriteLine($"Contains 'private agrint m_value1;': {result.Contains("private agrint m_value1;")}");
                    Console.WriteLine($"Contains 'private CString cValue1;': {result.Contains("private CString cValue1;")}");
                    Console.WriteLine($"Contains 'public CSample();': {result.Contains("public CSample();")}");
                }

                Assert.NotNull(cppClass);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }
    }
}