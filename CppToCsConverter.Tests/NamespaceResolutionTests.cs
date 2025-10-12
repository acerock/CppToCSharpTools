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
        [InlineData(@"config_v2", "U4.BatchNet.config_v2.Compatibility")]
        [InlineData(@"my-utils", "U4.BatchNet.my-utils.Compatibility")]
        [InlineData(@"test.folder", "U4.BatchNet.test.folder.Compatibility")]
        [InlineData(@"folder_with_underscores", "U4.BatchNet.folder_with_underscores.Compatibility")]
        public void ResolveNamespace_WithSpecialCharacters_ReturnsFullFolderName(string inputPath, string expectedNamespace)
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
    }
}