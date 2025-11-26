using Xunit;
using System.IO;
using System.Linq;
using CppToCsConverter.Core.Core;

namespace CppToCsConverter.Tests
{
    public class TypedefStructParsingTests
    {
        [Fact]
        public void TypedefStruct_WithPrecedingDefines_ShouldNotIncludeDefinesInMembers()
        {
            // Arrange - Create minimal test case
            var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);

            try
            {
                var headerContent = @"#pragma once

/* Message texts*/
#define MAX_AMT_TRANS 236 
#define MAX_AMT_PER 237

typedef struct
{
agrint lCalcDebTax;
agrint lCalcSocTax;
agrint lCurrentIdTrans;

TAttId attLegalEnt;
TAttId attPdRelAccount;

CString cDateFrom;  
CString cDateTo;
} GLOBALCONSTANTS;
";

                var headerPath = Path.Combine(tempDir, "Test.h");
                File.WriteAllText(headerPath, headerContent);

                // Act - Convert
                var converter = new CppToCsStructuralConverter();
                converter.ConvertDirectory(tempDir, tempDir);

                // Assert - Check generated file
                var csFilePath = Path.Combine(tempDir, "Test.cs");
                Assert.True(File.Exists(csFilePath), "Generated C# file should exist");

                var generated = File.ReadAllText(csFilePath);
                
                // The struct should have correct members
                Assert.Contains("internal agrint lCalcDebTax;", generated);
                Assert.Contains("internal agrint lCalcSocTax;", generated);
                Assert.Contains("internal TAttId attLegalEnt;", generated);
                Assert.Contains("internal CString cDateFrom;", generated);
                
                // Should NOT contain the typedef struct definition verbatim
                Assert.DoesNotContain("typedef struct", generated);
                
                // Should NOT have #define statements inside the class body
                Assert.DoesNotContain("#define MAX_AMT_TRANS", generated);
                Assert.DoesNotContain("#define MAX_AMT_PER", generated);
                
                // Debug output to see what was actually generated
                System.Console.WriteLine("=== GENERATED C# ===");
                System.Console.WriteLine(generated);
                System.Console.WriteLine("=== END ===");
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }
    }
}
