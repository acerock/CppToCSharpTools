using System;
using System.IO;
using Xunit;
using CppToCsConverter.Core.Core;

namespace CppToCsConverter.Tests
{
    public class LogicVerificationTest
    {
        [Fact]
        public void VerifyTestsUsesSameLogicAsApp()
        {
            // Arrange - Use same content as app test
            var tempDir = Path.Combine(Path.GetTempPath(), "TestLogic");
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
            Directory.CreateDirectory(tempDir);

            var headerContent = "class TestLogic { public: void InlineMethod() { /* inline */ } void SourceMethod(); };";
            var sourceContent = "void TestLogic::SourceMethod() { /* source */ }";

            var headerFile = Path.Combine(tempDir, "TestLogic.h");
            var sourceFile = Path.Combine(tempDir, "TestLogic.cpp");

            File.WriteAllText(headerFile, headerContent);
            File.WriteAllText(sourceFile, sourceContent);

            try
            {
                // Act - Use same converter as app
                var converter = new CppToCsStructuralConverter();
                converter.ConvertDirectory(tempDir, tempDir);

                // Assert - Check generated content matches app output
                var outputFile = Path.Combine(tempDir, "TestLogic.cs");
                Assert.True(File.Exists(outputFile));
                
                var generatedContent = File.ReadAllText(outputFile);
                
                // This should match exactly what the app generates
                Assert.Contains("namespace U4.BatchNet.TL.Compatibility", generatedContent);
                Assert.Contains("internal class TestLogic", generatedContent);
                Assert.Contains("SourceMethod()", generatedContent);
                Assert.Contains("/* source */", generatedContent);
                
                // Should not be partial (only source method, no inline detected)
                Assert.DoesNotContain("partial class", generatedContent);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }
    }
}