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
    public class InlineMethodDebugTests
    {
        private readonly CppHeaderParser _headerParser;
        private readonly CsClassGenerator _generator;

        public InlineMethodDebugTests()
        {
            _headerParser = new CppHeaderParser();
            _generator = new CsClassGenerator();
        }

        [Fact]
        public void Debug_MultiLineInlineMethods_ShouldBeParsed()
        {
            // Arrange - Test the exact content from CSample.h that's missing
            var headerContent = @"
class CSample
{
private:
    CString cValue1;

    CString PrivateMemberWithBodyInHfile(const TAttId &att_id)
    {
        if (cValue1.IsEmpty()) return _T("""");

        return cValue1;
    }

    int MethodPrivInl1(const TDimValue& dim1)
    {
        if (dim1.IsEmpty()) 
            return 0;
        
        return 42;
    }

    bool CSample::MethodPrivInl2(const TDimValue& dimPd, const agrint& lLimitHorizon, const agrint& iValue=0, bool bError=false)
    {
        if (dimPd.IsEmpty()) 
            return bError;

        return lLimitHorizon >= iValue;
    }

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
};";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, headerContent);

            try
            {
                // Act
                var classes = _headerParser.ParseHeaderFile(tempFile);
                var cppClass = classes.FirstOrDefault(c => c.Name == "CSample");
                
                Console.WriteLine("=== DEBUG: PARSED METHODS ===");
                Console.WriteLine($"Found class: {cppClass?.Name}");
                Console.WriteLine($"Method count: {cppClass?.Methods?.Count ?? 0}");
                
                if (cppClass != null)
                {
                    foreach (var method in cppClass.Methods)
                    {
                        Console.WriteLine($"Method: {method.Name}");
                        Console.WriteLine($"  - HasInlineImplementation: {method.HasInlineImplementation}");
                        Console.WriteLine($"  - InlineImplementation length: {method.InlineImplementation?.Length ?? 0}");
                        if (!string.IsNullOrEmpty(method.InlineImplementation))
                        {
                            Console.WriteLine($"  - InlineImplementation: {method.InlineImplementation.Substring(0, Math.Min(50, method.InlineImplementation.Length))}...");
                        }
                        Console.WriteLine();
                    }

                    var result = _generator.GenerateClass(cppClass, new List<CppMethod>(), "CSample");
                    
                    Console.WriteLine("=== DEBUG: GENERATED C# ===");
                    Console.WriteLine(result);
                    Console.WriteLine("=== END DEBUG ===");

                    // Assert - Check that all the inline methods are present
                    Assert.Contains("PrivateMemberWithBodyInHfile", result);
                    Assert.Contains("MethodPrivInl1", result);
                    Assert.Contains("MethodPrivInl2", result);
                    Assert.Contains("InlineMethodWithOverload", result);
                    
                    // Verify the inline implementations are included
                    Assert.Contains("if (cValue1.IsEmpty())", result);
                    Assert.Contains("if (dim1.IsEmpty())", result);
                    Assert.Contains("return 42", result);
                    Assert.Contains("return 100", result);
                }
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void Debug_RealCSampleFile_CheckInlineMethodParsing()
        {
            // Test the real CSample.h file to see what's happening
            var headerFile = @"d:\dev\CppToCSharpTools\Work\Sample\CSample.h";
            
            if (!File.Exists(headerFile))
            {
                Assert.Fail($"CSample.h file not found at {headerFile}");
                return;
            }

            // Act
            var classes = _headerParser.ParseHeaderFile(headerFile);
            var cppClass = classes.FirstOrDefault(c => c.Name == "CSample");
            
            Console.WriteLine("=== DEBUGGING REAL CSAMPLE.H FILE ===");
            Console.WriteLine($"Found class: {cppClass?.Name}");
            Console.WriteLine($"Method count: {cppClass?.Methods?.Count ?? 0}");
            Console.WriteLine();
            
            if (cppClass != null)
            {
                Console.WriteLine("=== ALL METHODS IN CSAMPLE CLASS ===");
                foreach (var method in cppClass.Methods)
                {
                    Console.WriteLine($"Method: {method.Name}");
                    Console.WriteLine($"  - HasInlineImplementation: {method.HasInlineImplementation}");
                    Console.WriteLine($"  - InlineImplementation length: {method.InlineImplementation?.Length ?? 0}");
                    Console.WriteLine($"  - ReturnType: {method.ReturnType}");
                    Console.WriteLine($"  - AccessSpecifier: {method.AccessSpecifier}");
                    if (!string.IsNullOrEmpty(method.InlineImplementation))
                    {
                        Console.WriteLine($"  - InlineImplementation: {method.InlineImplementation.Substring(0, Math.Min(100, method.InlineImplementation.Length))}...");
                    }
                    Console.WriteLine();
                }

                Console.WriteLine("=== GENERATING C# OUTPUT ===");
                var result = _generator.GenerateClass(cppClass, new List<CppMethod>(), "CSample");
                
                Console.WriteLine("=== CHECKING FOR MISSING INLINE METHODS ===");
                var expectedInlineMethods = new[] { 
                    "PrivateMemberWithBodyInHfile", 
                    "MethodPrivInl1", 
                    "MethodPrivInl2", 
                    "InlineMethodWithOverload" 
                };
                
                foreach (var expectedMethod in expectedInlineMethods)
                {
                    bool found = result.Contains(expectedMethod);
                    Console.WriteLine($"{expectedMethod}: {(found ? "FOUND" : "MISSING")}");
                }

                // Now verify the fix works - all inline methods should be present
                Assert.Contains("PrivateMemberWithBodyInHfile", result);
                Assert.Contains("MethodPrivInl1", result);  
                Assert.Contains("MethodPrivInl2", result);
                Assert.Contains("InlineMethodWithOverload", result);
            }
        }

        [Fact]
        public void InlineMethods_BothPublicAndPrivate_ShouldBeIncluded()
        {
            // This test verifies the fix for the issue where only public inline methods were included
            var headerContent = @"
class CSample
{
public:
    // Public inline method
    bool PublicInlineMethod() { return true; }
    
private:
    // Private inline method  
    int PrivateInlineMethod() { return 42; }
};";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, headerContent);

            try
            {
                // Act
                var classes = _headerParser.ParseHeaderFile(tempFile);
                var cppClass = classes.FirstOrDefault(c => c.Name == "CSample");
                Assert.NotNull(cppClass);

                var result = _generator.GenerateClass(cppClass, new List<CppMethod>(), "CSample");

                // Assert - Both public and private inline methods should be present
                Assert.Contains("PublicInlineMethod", result);
                Assert.Contains("PrivateInlineMethod", result);
                Assert.Contains("return true", result);
                Assert.Contains("return 42", result);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }
    }
}