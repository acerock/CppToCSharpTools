using CppToCsConverter.Core.Core;
using Xunit;

namespace CppToCsConverter.Tests
{
    public class NamespaceResolutionTests
    {
        private readonly CppToCsStructuralConverter _converter;

        public NamespaceResolutionTests()
        {
            _converter = new CppToCsStructuralConverter();
        }

        [Theory]
        [InlineData(@"D:\Dev\CppToCSharpTools\Work\AgrLibHS", "U4.BatchNet.HS.Compatibility")]
        [InlineData(@"C:\Projects\AgrLibHS", "U4.BatchNet.HS.Compatibility")]
        [InlineData(@"AgrLibHS", "U4.BatchNet.HS.Compatibility")]
        [InlineData(@"TestABCD", "U4.BatchNet.CD.Compatibility")]
        [InlineData(@"MyProjectXYZ", "U4.BatchNet.YZ.Compatibility")]
        [InlineData(@"SomeLibABC", "U4.BatchNet.BC.Compatibility")]
        public void ResolveNamespace_WithTwoOrMoreUppercaseChars_ReturnsLastTwoUppercaseChars(string inputPath, string expectedNamespace)
        {
            // Act
            var result = _converter.ResolveNamespace(inputPath);

            // Assert
            Assert.Equal(expectedNamespace, result);
        }

        [Theory]
        [InlineData(@"D:\Dev\CppToCSharpTools\Work\Sample", "U4.BatchNet.Sample.Compatibility")]
        [InlineData(@"C:\Projects\Sample", "U4.BatchNet.Sample.Compatibility")]
        [InlineData(@"Sample", "U4.BatchNet.Sample.Compatibility")]
        [InlineData(@"Config", "U4.BatchNet.Config.Compatibility")]
        [InlineData(@"Utils", "U4.BatchNet.Utils.Compatibility")]
        [InlineData(@"Test", "U4.BatchNet.Test.Compatibility")]
        [InlineData(@"Main", "U4.BatchNet.Main.Compatibility")]
        public void ResolveNamespace_WithOneUppercaseChar_ReturnsFullFolderName(string inputPath, string expectedNamespace)
        {
            // Act
            var result = _converter.ResolveNamespace(inputPath);

            // Assert
            Assert.Equal(expectedNamespace, result);
        }

        [Theory]
        [InlineData(@"D:\Dev\CppToCSharpTools\Work\lowercasetest", "U4.BatchNet.lowercasetest.Compatibility")]
        [InlineData(@"C:\Projects\utils", "U4.BatchNet.utils.Compatibility")]
        [InlineData(@"lowercasetest", "U4.BatchNet.lowercasetest.Compatibility")]
        [InlineData(@"config", "U4.BatchNet.config.Compatibility")]
        [InlineData(@"helpers", "U4.BatchNet.helpers.Compatibility")]
        [InlineData(@"common", "U4.BatchNet.common.Compatibility")]
        public void ResolveNamespace_WithNoUppercaseChars_ReturnsFullFolderName(string inputPath, string expectedNamespace)
        {
            // Act
            var result = _converter.ResolveNamespace(inputPath);

            // Assert
            Assert.Equal(expectedNamespace, result);
        }

        [Theory]
        [InlineData("", "Generated_Unknown")]
        public void ResolveNamespace_WithEmptyInput_ReturnsGeneratedUnknown(string inputPath, string expectedNamespace)
        {
            // Act
            var result = _converter.ResolveNamespace(inputPath);

            // Assert
            Assert.Equal(expectedNamespace, result);
        }

        [Fact]
        public void ResolveNamespace_WithNullInput_ReturnsGeneratedUnknown()
        {
            // Act
            var result = _converter.ResolveNamespace(null!);

            // Assert
            Assert.Equal("Generated_Unknown", result);
        }

        [Theory]
        [InlineData(@"D:\Dev\CppToCSharpTools\Work\", "U4.BatchNet.Work.Compatibility")]  // Extracts "Work" from path
        [InlineData(@"C:\Projects\Sample\", "U4.BatchNet.Sample.Compatibility")]  // Extracts "Sample" from path
        [InlineData(@"D:\Dev\CppToCSharpTools\Work\AgrLibHS\", "U4.BatchNet.HS.Compatibility")]  // Extracts "AgrLibHS" -> HS
        public void ResolveNamespace_WithTrailingSlash_ExtractsFolderNameCorrectly(string inputPath, string expectedNamespace)
        {
            // Act
            var result = _converter.ResolveNamespace(inputPath);

            // Assert
            Assert.Equal(expectedNamespace, result);
        }

        [Theory]
        [InlineData("   ", "U4.BatchNet.   .Compatibility")]  // Whitespace is treated as folder name
        [InlineData("\t", "U4.BatchNet.\t.Compatibility")]  // Tab is treated as folder name
        public void ResolveNamespace_WithWhitespaceInput_TreatsAsFolder(string inputPath, string expectedNamespace)
        {
            // Act
            var result = _converter.ResolveNamespace(inputPath);

            // Assert
            Assert.Equal(expectedNamespace, result);
        }

        [Theory]
        [InlineData(@"TestLogic", "U4.BatchNet.TL.Compatibility")] // T and L
        [InlineData(@"MyTestClass", "U4.BatchNet.TC.Compatibility")] // T and C (last two)
        [InlineData(@"ABCDEFTest", "U4.BatchNet.FT.Compatibility")] // F and T (last two from A,B,C,D,E,F,T)
        [InlineData(@"AgrLibHSTest", "U4.BatchNet.ST.Compatibility")] // S and T (last two from A,L,H,S,T)
        public void ResolveNamespace_WithMultipleUppercaseChars_ReturnsCorrectLastTwo(string inputPath, string expectedNamespace)
        {
            // Act
            var result = _converter.ResolveNamespace(inputPath);

            // Assert
            Assert.Equal(expectedNamespace, result);
        }

        [Theory]
        [InlineData(@"test123", "U4.BatchNet.test123.Compatibility")]
        [InlineData(@"config_v2", "U4.BatchNet.v2.Compatibility")]  // After underscore: v2
        [InlineData(@"my-utils", "U4.BatchNet.utils.Compatibility")] // After dash: utils
        [InlineData(@"test.folder", "U4.BatchNet.folder.Compatibility")] // After dot: folder
        [InlineData(@"folder_with_underscores", "U4.BatchNet.underscores.Compatibility")] // After last underscore: underscores
        public void ResolveNamespace_WithSpecialCharacters_ReturnsTrailingPart(string inputPath, string expectedNamespace)
        {
            // Act
            var result = _converter.ResolveNamespace(inputPath);

            // Assert
            Assert.Equal(expectedNamespace, result);
        }

        [Theory]
        [InlineData(@"Test123ABC", "U4.BatchNet.BC.Compatibility")] // A, B, C - last two are B, C
        [InlineData(@"myTestABC", "U4.BatchNet.BC.Compatibility")] // T, A, B, C - last two are B, C
        [InlineData(@"ConfigXY", "U4.BatchNet.XY.Compatibility")] // C, X, Y - last two are X, Y
        [InlineData(@"123TestAB", "U4.BatchNet.AB.Compatibility")] // T, A, B - last two are A, B
        public void ResolveNamespace_WithMixedCaseAndNumbers_ReturnsCorrectLastTwoUppercase(string inputPath, string expectedNamespace)
        {
            // Act
            var result = _converter.ResolveNamespace(inputPath);

            // Assert
            Assert.Equal(expectedNamespace, result);
        }

        [Theory]
        [InlineData(@"D:\Dev\CppToCSharpTools\Work\AgrLibHS\", "U4.BatchNet.HS.Compatibility")] // With trailing slash
        [InlineData(@"D:\Dev\CppToCSharpTools\Work\AgrLibHS\\", "U4.BatchNet.HS.Compatibility")] // With double trailing slash
        [InlineData(@"D:/Dev/CppToCSharpTools/Work/Sample/", "U4.BatchNet.Sample.Compatibility")] // Unix-style path with trailing slash
        [InlineData(@"D:/Dev/CppToCSharpTools/Work/Sample", "U4.BatchNet.Sample.Compatibility")] // Unix-style path without trailing slash
        public void ResolveNamespace_WithDifferentPathFormats_HandlesCorrectly(string inputPath, string expectedNamespace)
        {
            // Act
            var result = _converter.ResolveNamespace(inputPath);

            // Assert
            Assert.Equal(expectedNamespace, result);
        }

        [Fact]
        public void ResolveNamespace_WithRealWorldExamples_ProducesExpectedResults()
        {
            // Test cases based on common project folder names
            var testCases = new[]
            {
                // Real-world folder names with >= 2 uppercase chars (should use last 2)
                ("AgressoLibHS", "U4.BatchNet.HS.Compatibility"),
                ("BatchNetSX", "U4.BatchNet.SX.Compatibility"), 
                ("CoreLibAPI", "U4.BatchNet.PI.Compatibility"),
                ("TestFrameworkXY", "U4.BatchNet.XY.Compatibility"),
                
                // Real-world folder names with < 2 uppercase chars (should use full name)
                ("sample", "U4.BatchNet.sample.Compatibility"),
                ("config", "U4.BatchNet.config.Compatibility"),
                ("Core", "U4.BatchNet.Core.Compatibility"),
                ("Tests", "U4.BatchNet.Tests.Compatibility"),
                ("Utils", "U4.BatchNet.Utils.Compatibility"),
                ("Models", "U4.BatchNet.Models.Compatibility"),
                
                // Edge cases
                ("A", "U4.BatchNet.A.Compatibility"),
                ("AB", "U4.BatchNet.AB.Compatibility"),
                ("ABC", "U4.BatchNet.BC.Compatibility"),
                ("ABCD", "U4.BatchNet.CD.Compatibility")
            };

            foreach (var (input, expected) in testCases)
            {
                // Act
                var result = _converter.ResolveNamespace(input);

                // Assert
                Assert.Equal(expected, result);
            }
        }

        [Theory]
        [InlineData(@"Something.AgrXY", "U4.BatchNet.XY.Compatibility")] // After dot: AgrXY -> XY
        [InlineData(@"Something_Sample", "U4.BatchNet.Sample.Compatibility")] // After underscore: Sample (< 2 uppercase)
        [InlineData(@"Project-TestLib", "U4.BatchNet.TL.Compatibility")] // After dash: TestLib -> TL
        [InlineData(@"Prefix.LibHS", "U4.BatchNet.HS.Compatibility")] // After dot: LibHS -> HS
        [InlineData(@"Company_AgrLibHS", "U4.BatchNet.HS.Compatibility")] // After underscore: AgrLibHS -> HS
        [InlineData(@"Module-ConfigXY", "U4.BatchNet.XY.Compatibility")] // After dash: ConfigXY -> XY
        public void ResolveNamespace_WithSingleSeparator_ConsidersTrailingCharacters(string inputPath, string expectedNamespace)
        {
            // Act
            var result = _converter.ResolveNamespace(inputPath);

            // Assert
            Assert.Equal(expectedNamespace, result);
        }

        [Theory]
        [InlineData(@"Prefix.Module_TestAB", "U4.BatchNet.AB.Compatibility")] // Last separator is underscore: TestAB -> AB
        [InlineData(@"Company_Lib.ConfigHS", "U4.BatchNet.HS.Compatibility")] // Last separator is dot: ConfigHS -> HS
        [InlineData(@"Root-Sub_Final.TestXY", "U4.BatchNet.XY.Compatibility")] // Last separator is dot: TestXY -> XY
        [InlineData(@"A.B_C-Sample", "U4.BatchNet.Sample.Compatibility")] // Last separator is dash: Sample (< 2 uppercase)
        [InlineData(@"Deep.Path_With-ManyABC", "U4.BatchNet.BC.Compatibility")] // Last separator is dash: ManyABC -> BC (last 2)
        public void ResolveNamespace_WithMultipleSeparators_UsesLastSeparator(string inputPath, string expectedNamespace)
        {
            // Act
            var result = _converter.ResolveNamespace(inputPath);

            // Assert
            Assert.Equal(expectedNamespace, result);
        }

        [Theory]
        [InlineData(@"Something.sample", "U4.BatchNet.sample.Compatibility")] // After dot: sample (no uppercase)
        [InlineData(@"Prefix_config", "U4.BatchNet.config.Compatibility")] // After underscore: config (no uppercase)
        [InlineData(@"Module-utils", "U4.BatchNet.utils.Compatibility")] // After dash: utils (no uppercase)
        [InlineData(@"Company.Test", "U4.BatchNet.Test.Compatibility")] // After dot: Test (1 uppercase)
        [InlineData(@"Project_Core", "U4.BatchNet.Core.Compatibility")] // After underscore: Core (1 uppercase)
        public void ResolveNamespace_WithSeparatorsAndLowercaseTrailing_ReturnsFullTrailingPart(string inputPath, string expectedNamespace)
        {
            // Act
            var result = _converter.ResolveNamespace(inputPath);

            // Assert
            Assert.Equal(expectedNamespace, result);
        }

        [Theory]
        [InlineData(@"D:\test\Something.AgrXY", "U4.BatchNet.XY.Compatibility")] // Full path with dot separator
        [InlineData(@"C:\Projects\Something_Sample", "U4.BatchNet.Sample.Compatibility")] // Full path with underscore
        [InlineData(@"D:\Dev\Project-TestLib", "U4.BatchNet.TL.Compatibility")] // Full path with dash
        [InlineData(@"C:\Work\Deep.Path_Final-TestABC", "U4.BatchNet.BC.Compatibility")] // Full path, multiple separators
        public void ResolveNamespace_WithFullPathsAndSeparators_ExtractsCorrectTrailingPart(string inputPath, string expectedNamespace)
        {
            // Act
            var result = _converter.ResolveNamespace(inputPath);

            // Assert
            Assert.Equal(expectedNamespace, result);
        }

        [Theory]
        [InlineData(@"Project.", "U4.BatchNet..Compatibility")] // Ends with dot - empty trailing part
        [InlineData(@"Project_", "U4.BatchNet..Compatibility")] // Ends with underscore - empty trailing part  
        [InlineData(@"Project-", "U4.BatchNet..Compatibility")] // Ends with dash - empty trailing part
        [InlineData(@"A.B_C-", "U4.BatchNet..Compatibility")] // Ends with separator - empty trailing part
        public void ResolveNamespace_WithTrailingSeparators_HandlesEmptyTrailingPart(string inputPath, string expectedNamespace)
        {
            // Act
            var result = _converter.ResolveNamespace(inputPath);

            // Assert
            Assert.Equal(expectedNamespace, result);
        }

        [Theory]
        [InlineData(@"No-Separators-Here", "U4.BatchNet.Here.Compatibility")] // After dash: "Here" has 1 uppercase (H)
        [InlineData(@"Multiple.Dots.In.Path", "U4.BatchNet.Path.Compatibility")] // After dot: "Path" has 1 uppercase
        [InlineData(@"Under_Score_Every_Where", "U4.BatchNet.Where.Compatibility")] // After underscore: "Where" has 1 uppercase  
        [InlineData(@"Mix.Of_Different-Separators", "U4.BatchNet.Separators.Compatibility")] // After dash: "Separators" has 1 uppercase
        public void ResolveNamespace_WithComplexSeparatorPatterns_HandlesProperly(string inputPath, string expectedNamespace)
        {
            // Act
            var result = _converter.ResolveNamespace(inputPath);

            // Assert
            Assert.Equal(expectedNamespace, result);
        }

        [Fact]
        public void ResolveNamespace_WithReadmeExamples_ProducesCorrectResults()
        {
            // Test cases directly from README.md requirements
            var readmeTestCases = new[]
            {
                // From README: "Something.AgrXY" should become "U4.BatchNet.XY.Compatibility"
                (@"c:\test\Something.AgrXY", "U4.BatchNet.XY.Compatibility"),
                (@"Something.AgrXY", "U4.BatchNet.XY.Compatibility"),
                
                // From README: "Something_Sample" should become "U4.BatchNet.Sample.Compatibility"
                (@"c:\test\Something_Sample", "U4.BatchNet.Sample.Compatibility"),
                (@"Something_Sample", "U4.BatchNet.Sample.Compatibility"),
                
                // Test with different separators to verify last occurrence logic
                (@"Project-TestLib", "U4.BatchNet.TL.Compatibility"), // TestLib -> TL (2 uppercase chars)
                (@"Deep.Path_Final-TestXY", "U4.BatchNet.XY.Compatibility"), // TestXY after last separator (dash)
                
                // Edge cases with multiple separators
                (@"A.B_C-ConfigHS", "U4.BatchNet.HS.Compatibility"), // ConfigHS after last separator
                (@"Root_Sub.Final-Sample", "U4.BatchNet.Sample.Compatibility"), // Sample after last separator
                
                // Test path scenarios
                (@"D:\Dev\Project.AgrLibXY", "U4.BatchNet.XY.Compatibility"),
                (@"C:\Work\Module_TestAB", "U4.BatchNet.AB.Compatibility"),
                (@"D:\Projects\Deep-Path_Config", "U4.BatchNet.Config.Compatibility"),
                
                // Test cases where trailing part has < 2 uppercase chars
                (@"Something.sample", "U4.BatchNet.sample.Compatibility"),
                (@"Project_config", "U4.BatchNet.config.Compatibility"),
                (@"Module-utils", "U4.BatchNet.utils.Compatibility"),
                (@"Deep.Path_Test", "U4.BatchNet.Test.Compatibility"), // Test has 1 uppercase
            };

            foreach (var (inputPath, expected) in readmeTestCases)
            {
                // Act
                var result = _converter.ResolveNamespace(inputPath);

                // Assert
                Assert.Equal(expected, result);
            }
        }

        [Theory]
        [InlineData(@"Something.AgrLibHS", "U4.BatchNet.HS.Compatibility")] // From AgrLibHS -> HS
        [InlineData(@"c:\test\AgrLibHS", "U4.BatchNet.HS.Compatibility")] // Original example without separators
        [InlineData(@"c:\test\AgrYX", "U4.BatchNet.YX.Compatibility")] // Original example without separators  
        [InlineData(@"Prefix.Module_TestConfigABC", "U4.BatchNet.BC.Compatibility")] // TestConfigABC -> BC (last 2 uppercase)
        [InlineData(@"Company_Lib.Final-AgrXYZ", "U4.BatchNet.YZ.Compatibility")] // AgrXYZ -> YZ (last 2 uppercase)
        public void ResolveNamespace_WithReadmePatternVariations_HandlesCorrectly(string inputPath, string expectedNamespace)
        {
            // Act  
            var result = _converter.ResolveNamespace(inputPath);

            // Assert
            Assert.Equal(expectedNamespace, result);
        }

        [Theory]
        [InlineData(@"Test.Config_Utils-final", "U4.BatchNet.final.Compatibility")] // No uppercase in "final"
        [InlineData(@"Deep-Path.Module_test123", "U4.BatchNet.test123.Compatibility")] // No uppercase in "test123"
        [InlineData(@"Project_Sub-config-v2", "U4.BatchNet.v2.Compatibility")] // No uppercase in "v2"
        [InlineData(@"A.B_C-D.E_helper", "U4.BatchNet.helper.Compatibility")] // No uppercase in "helper" 
        public void ResolveNamespace_WithSeparatorsAndNoUppercaseInTrailing_UsesFullTrailingPart(string inputPath, string expectedNamespace)
        {
            // Act
            var result = _converter.ResolveNamespace(inputPath);

            // Assert
            Assert.Equal(expectedNamespace, result);
        }
    }
}