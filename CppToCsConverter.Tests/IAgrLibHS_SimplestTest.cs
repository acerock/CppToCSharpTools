using Xunit;
using System.IO;
using System.Linq;
using CppToCsConverter.Core.Parsers;
using CppToCsConverter.Core;

namespace CppToCsConverter.Tests
{
    /// <summary>
    /// Tests for IAgrLibHS_simplest.h - a minimal reproduction of the corruption issue
    /// This test validates both parsing and generation in separate steps
    /// </summary>
    public class IAgrLibHSSimplestTest
    {
        private const string SimplestHeaderPath = @"D:\BatchNetTools\CppToCSharpTools\Work\AgrLibHS\IAgrLibHS_simplest.h";
        
        [Fact]
        public void Step1_Parse_ShouldHave_CorrectStructCount()
        {
            // Arrange & Act
            if (!File.Exists(SimplestHeaderPath))
            {
                // Skip if file doesn't exist
                return;
            }

            var parser = new CppHeaderParser();
            var classes = parser.ParseHeaderFile(SimplestHeaderPath);
            
            // Assert - Should find 4 types total
            Assert.Equal(4, classes.Count);
            
            // Should have 2 structs, 1 interface, 1 struct
            var structs = classes.Where(c => c.IsStruct).ToList();
            var interfaces = classes.Where(c => c.IsInterface).ToList();
            
            Assert.Equal(3, structs.Count); // GLOBALCONSTANTS, ErrorParameters, PRXDEF
            Assert.Single(interfaces); // IAgrLibHS
            
            System.Console.WriteLine($"✓ Parsed {classes.Count} types: {structs.Count} structs, {interfaces.Count} interface");
        }
        
        [Fact]
        public void Step2_Parse_GLOBALCONSTANTS_ShouldHave_CorrectMembers()
        {
            // Arrange & Act
            if (!File.Exists(SimplestHeaderPath))
            {
                return;
            }

            var parser = new CppHeaderParser();
            var classes = parser.ParseHeaderFile(SimplestHeaderPath);
            var globalConstants = classes.FirstOrDefault(c => c.Name == "GLOBALCONSTANTS");
            
            // Assert
            Assert.NotNull(globalConstants);
            Assert.True(globalConstants.IsStruct);
            
            // Should have exactly 4 members
            Assert.Equal(4, globalConstants.Members.Count);
            
            // Verify member names
            Assert.Equal("lCalcDebTax", globalConstants.Members[0].Name);
            Assert.Equal("attPrnLFRelId", globalConstants.Members[1].Name);
            Assert.Equal("attCustomId1", globalConstants.Members[2].Name);
            Assert.Equal("cntTransId", globalConstants.Members[3].Name);
            
            // Verify member types
            Assert.Equal("agrint", globalConstants.Members[0].Type);
            Assert.Equal("TAttId", globalConstants.Members[1].Type);
            Assert.Equal("TAttId", globalConstants.Members[2].Type);
            Assert.Equal("TCounter", globalConstants.Members[3].Type);
            
            System.Console.WriteLine($"✓ GLOBALCONSTANTS has {globalConstants.Members.Count} members (correct)");
        }
        
        [Fact]
        public void Step3_Parse_GLOBALCONSTANTS_Members_ShouldNotContain_RawCppCode()
        {
            // Arrange & Act
            if (!File.Exists(SimplestHeaderPath))
            {
                return;
            }

            var parser = new CppHeaderParser();
            var classes = parser.ParseHeaderFile(SimplestHeaderPath);
            var globalConstants = classes.FirstOrDefault(c => c.Name == "GLOBALCONSTANTS");
            
            Assert.NotNull(globalConstants);
            
            // Assert - Check each member property for contamination
            foreach (var member in globalConstants.Members)
            {
                // Member Type should not contain C++ keywords
                Assert.DoesNotContain("#define", member.Type);
                Assert.DoesNotContain("typedef", member.Type);
                Assert.DoesNotContain("struct", member.Type);
                
                // Member Name should not contain C++ keywords
                Assert.DoesNotContain("#define", member.Name);
                Assert.DoesNotContain("typedef", member.Name);
                
                // PostfixComment should not contain raw C++ code (only actual comments)
                if (!string.IsNullOrEmpty(member.PostfixComment))
                {
                    Assert.DoesNotContain("#define", member.PostfixComment);
                    Assert.DoesNotContain("typedef struct", member.PostfixComment);
                }
                
                // PrecedingComments should not contain raw C++ code
                foreach (var comment in member.PrecedingComments)
                {
                    System.Console.WriteLine($"    Member '{member.Name}' PrecedingComment: '{comment}'");
                    
                    // Comments can contain the WORDS "define" or "typedef" in explanatory text,
                    // but not the actual C++ syntax
                    if (comment.Contains("#define") || comment.Contains("typedef struct {"))
                    {
                        System.Console.WriteLine($"ERROR: Member '{member.Name}' PrecedingComment contains raw C++: {comment}");
                        Assert.Fail($"PrecedingComment should not contain raw C++ syntax");
                    }
                }
                
                // RegionStart and RegionEnd should not contain raw C++ code
                Assert.DoesNotContain("#define", member.RegionStart ?? "");
                Assert.DoesNotContain("typedef", member.RegionStart ?? "");
                Assert.DoesNotContain("#define", member.RegionEnd ?? "");
                Assert.DoesNotContain("typedef struct", member.RegionEnd ?? "");
            }
            
            System.Console.WriteLine($"✓ All {globalConstants.Members.Count} members are clean (no raw C++ contamination)");
        }
        
        [Fact]
        public void Step4_Parse_GLOBALCONSTANTS_HeaderDefines_ShouldBeEmpty()
        {
            // Arrange & Act
            if (!File.Exists(SimplestHeaderPath))
            {
                return;
            }

            var parser = new CppHeaderParser();
            var classes = parser.ParseHeaderFile(SimplestHeaderPath);
            var globalConstants = classes.FirstOrDefault(c => c.Name == "GLOBALCONSTANTS");
            
            Assert.NotNull(globalConstants);
            
            // Assert - Structs should NOT have HeaderDefines
            Assert.Empty(globalConstants.HeaderDefines);
            
            System.Console.WriteLine($"✓ GLOBALCONSTANTS.HeaderDefines is empty (correct - structs should not get defines)");
        }
        
        [Fact]
        public void Step5_Parse_IAgrLibHS_Interface_ShouldHave_HeaderDefines()
        {
            // Arrange & Act
            if (!File.Exists(SimplestHeaderPath))
            {
                return;
            }

            var parser = new CppHeaderParser();
            var classes = parser.ParseHeaderFile(SimplestHeaderPath);
            var iAgrLibHS = classes.FirstOrDefault(c => c.Name == "IAgrLibHS");
            
            Assert.NotNull(iAgrLibHS);
            Assert.True(iAgrLibHS.IsInterface);
            Assert.True(iAgrLibHS.IsPublicExport);
            
            // Assert - The interface SHOULD get the header defines
            Assert.NotEmpty(iAgrLibHS.HeaderDefines);
            Assert.Single(iAgrLibHS.HeaderDefines); // Should have LIM_TRANSACTION
            Assert.Equal("LIM_TRANSACTION", iAgrLibHS.HeaderDefines[0].Name);
            Assert.Equal("7", iAgrLibHS.HeaderDefines[0].Value);
            
            System.Console.WriteLine($"✓ IAgrLibHS.HeaderDefines has {iAgrLibHS.HeaderDefines.Count} define (correct)");
        }
        
        [Fact]
        public void Step6_Generation_TempFolder_ShouldNotContain_RawCppInGLOBALCONSTANTS()
        {
            // Arrange
            if (!File.Exists(SimplestHeaderPath))
            {
                return;
            }

            var tempOutputDir = Path.Combine(Path.GetTempPath(), $"CppToCsTest_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempOutputDir);
            
            try
            {
                // Act - Run full conversion
                var sourceDir = Path.GetDirectoryName(SimplestHeaderPath) ?? SimplestHeaderPath;
                var converter = new CppToCsConverter.Core.Core.CppToCsStructuralConverter();
                converter.ConvertSpecificFiles(sourceDir, new[] { "IAgrLibHS_simplest.h" }, tempOutputDir);
                
                // Assert - Check generated file
                var generatedFile = Path.Combine(tempOutputDir, "IAgrLibHS_simplest.cs");
                Assert.True(File.Exists(generatedFile), $"Expected file not generated: {generatedFile}");
                
                var content = File.ReadAllText(generatedFile);
                
                // Find GLOBALCONSTANTS class
                var globalConstantsStart = content.IndexOf("internal class GLOBALCONSTANTS");
                Assert.True(globalConstantsStart >= 0, "GLOBALCONSTANTS class not found in generated file");
                
                // Find next class (ErrorParameters)
                var errorParametersStart = content.IndexOf("internal class ErrorParameters", globalConstantsStart);
                Assert.True(errorParametersStart >= 0, "ErrorParameters class not found in generated file");
                
                // Extract GLOBALCONSTANTS class content
                var globalConstantsContent = content.Substring(globalConstantsStart, errorParametersStart - globalConstantsStart);
                
                System.Console.WriteLine($"=== GLOBALCONSTANTS class content ({globalConstantsContent.Length} chars) ===");
                System.Console.WriteLine(globalConstantsContent);
                System.Console.WriteLine("=== END ===");
                
                // Assert - Should NOT contain raw C++ code
                Assert.DoesNotContain("#define LIM_TRANSACTION", globalConstantsContent);
                Assert.DoesNotContain("typedef struct", globalConstantsContent);
                
                // Assert - Should contain the 4 expected members
                Assert.Contains("internal agrint lCalcDebTax;", globalConstantsContent);
                Assert.Contains("internal TAttId attPrnLFRelId;", globalConstantsContent);
                Assert.Contains("internal TAttId attCustomId1;", globalConstantsContent);
                Assert.Contains("internal TCounter cntTransId;", globalConstantsContent);
                
                // Assert - Should NOT contain duplicate member declarations
                var lCalcDebTaxCount = CountOccurrences(globalConstantsContent, "lCalcDebTax");
                var attPrnLFRelIdCount = CountOccurrences(globalConstantsContent, "attPrnLFRelId");
                
                Assert.Equal(1, lCalcDebTaxCount);
                Assert.Equal(1, attPrnLFRelIdCount);
                
                System.Console.WriteLine($"✓ GLOBALCONSTANTS class is clean (no raw C++ code)");
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(tempOutputDir))
                {
                    Directory.Delete(tempOutputDir, true);
                }
            }
        }
        
        private int CountOccurrences(string text, string search)
        {
            int count = 0;
            int index = 0;
            while ((index = text.IndexOf(search, index)) != -1)
            {
                count++;
                index += search.Length;
            }
            return count;
        }
    }
}
