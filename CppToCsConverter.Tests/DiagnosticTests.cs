using System;
using System.IO;
using System.Linq;
using Xunit;
using CppToCsConverter.Core.Generators;
using CppToCsConverter.Core.Parsers;

namespace CppToCsConverter.Tests
{
    /// <summary>
    /// Diagnostic test to understand why other tests are failing
    /// </summary>
    public class DiagnosticTests
    {
        [Fact]
        public void DiagnoseTypeConverter_ShowActualBehavior()
        {
            // Arrange
            var converter = new TypeConverter();
            
            // Act & Assert - Let's see what it actually returns
            var stdString = converter.ConvertType("std::string");
            var stdVector = converter.ConvertType("std::vector<int>");
            var dword = converter.ConvertType("DWORD");
            
            // Print actual values for debugging
            Console.WriteLine($"std::string -> '{stdString}'");
            Console.WriteLine($"std::vector<int> -> '{stdVector}'");
            Console.WriteLine($"DWORD -> '{dword}'");
            
            // These should pass based on current implementation
            Assert.Equal("std::string", stdString); // It preserves unknown types
            Assert.Equal("uint", dword); // This should work
        }
        
        [Fact]
        public void DiagnoseClassGeneration_ShowActualOutput()
        {
            // Arrange
            var generator = new CsClassGenerator();
            var headerParser = new CppHeaderParser();
            
            var headerContent = @"
class CSample
{
private:
    agrint m_value1;
public:
    void TestMethod();
};";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, headerContent);

            try
            {
                // Act
                var classes = headerParser.ParseHeaderFile(tempFile);
                var cppClass = classes.FirstOrDefault(c => c.Name == "CSample");
                
                Console.WriteLine($"Found class: {cppClass?.Name}");
                Console.WriteLine($"Members count: {cppClass?.Members?.Count ?? 0}");
                Console.WriteLine($"Methods count: {cppClass?.Methods?.Count ?? 0}");
                
                if (cppClass != null)
                {
                    Console.WriteLine("Members:");
                    foreach (var member in cppClass.Members)
                    {
                        Console.WriteLine($"  - {member.AccessSpecifier} {member.Type} {member.Name}");
                    }
                    
                    Console.WriteLine("Methods:");
                    foreach (var method in cppClass.Methods)
                    {
                        Console.WriteLine($"  - {method.AccessSpecifier} {method.ReturnType} {method.Name}()");
                    }
                    
                    var result = generator.GenerateClass(cppClass, new System.Collections.Generic.List<CppToCsConverter.Core.Models.CppMethod>(), "CSample");
                    Console.WriteLine($"Generated code length: {result.Length}");
                    Console.WriteLine($"Generated code:\n{result}");
                }
                
                Assert.NotNull(cppClass);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }
        
        [Fact]
        public void DiagnoseErrorHandling_ShowActualBehavior()
        {
            // Arrange
            var headerParser = new CppHeaderParser();
            var sourceParser = new CppSourceParser();
            var nonExistentFile = "C:\\NonExistent\\File.h";
            
            // Act - Let's see what actually happens
            var headerResult = headerParser.ParseHeaderFile(nonExistentFile);
            var sourceResult = sourceParser.ParseSourceFile(nonExistentFile);
            
            // The current implementation catches exceptions and returns empty results
            Console.WriteLine($"Header parser returned {headerResult.Count} classes for missing file");
            Console.WriteLine($"Source parser returned {sourceResult.Methods.Count} methods for missing file");
            
            // These will pass - the parsers don't throw, they return empty results
            Assert.Empty(headerResult);
            Assert.Empty(sourceResult.Methods);
        }
    }
}