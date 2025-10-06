using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CppToCsConverter.Core.Parsers;
using CppToCsConverter.Core.Generators;
using CppToCsConverter.Core.Models;
using Xunit;

namespace CppToCsConverter.Tests
{
    public class OverloadedInlineMethodTests
    {
        private readonly CppHeaderParser _headerParser;
        private readonly CsClassGenerator _generator;

        public OverloadedInlineMethodTests()
        {
            _headerParser = new CppHeaderParser();
            _generator = new CsClassGenerator();
        }

        [Fact]
        public void OverloadedInlineMethods_ShouldAllBeIncluded()
        {
            // Test case specifically for overloaded inline methods like InlineMethodWithOverload
            var headerContent = @"
class CSample
{
private:
    int InlineMethodWithOverload(const TDimValue& dim1)
    {
        if (dim1.IsEmpty()) 
            return -1;
        return 100;
    }

    int InlineMethodWithOverload(const TDimValue& dim1, bool bFlag, const CString& cPar = _T(""xyz""))
    {
        if (dim1.IsEmpty() || cPar == _T(""xyz"") || !bFlag)
            return -2;
        return 200;
    }

    int InlineMethodWithOverload(const TDimValue& dim1, bool bFlag, int i = 3)
    {
        if (dim1.IsEmpty()) 
            return -3;
        return 300;
    }
};";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, headerContent);

            try
            {
                // Act
                var classes = _headerParser.ParseHeaderFile(tempFile);
                var cppClass = classes.FirstOrDefault(c => c.Name == "CSample");
                Assert.NotNull(cppClass);

                Console.WriteLine($"=== PARSED OVERLOADED METHODS ({cppClass.Methods.Count}) ===");
                foreach (var method in cppClass.Methods)
                {
                    Console.WriteLine($"Method: {method.Name}");
                    Console.WriteLine($"  - Parameters: {method.Parameters.Count}");
                    Console.WriteLine($"  - HasInlineImplementation: {method.HasInlineImplementation}");
                    Console.WriteLine($"  - Implementation length: {method.InlineImplementation?.Length ?? 0}");
                    Console.WriteLine();
                }

                var result = _generator.GenerateClass(cppClass, new List<CppMethod>(), "CSample");
                
                Console.WriteLine("=== GENERATED C# ===");
                Console.WriteLine(result);

                // Assert - All 3 overloads should be present
                var overloadMatches = System.Text.RegularExpressions.Regex.Matches(result, @"InlineMethodWithOverload");
                Assert.Equal(3, overloadMatches.Count); // Should find exactly 3 overloads

                // Verify specific return values are present
                Assert.Contains("return 100", result); // First overload
                Assert.Contains("return 200", result); // Second overload  
                Assert.Contains("return 300", result); // Third overload
            }
            finally
            {
                File.Delete(tempFile);
            }
        }
    }
}