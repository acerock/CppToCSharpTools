using System;
using System.IO;
using CppToCsConverter.Core;
using CppToCsConverter.Core.Core;
using Xunit;

namespace CppToCsConverter.Tests
{
    public class RealWorldLocalStructTest
    {
        [Fact]
        public void CPartialSampleMethods_LocalStruct_ShouldNotBeExtracted()
        {
            // Use the EXACT content from CPartialSampleMethods.cpp
            var headerContent = File.ReadAllText(@"D:\BatchNetTools\CppToCSharpTools\SamplesAndExpectations\CPartialSample.h");
            var sourceContent = File.ReadAllText(@"D:\BatchNetTools\CppToCSharpTools\SamplesAndExpectations\CPartialSampleMethods.cpp");

            var tempDir = Path.Combine(Path.GetTempPath(), "RealWorldTest_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            var headerFile = Path.Combine(tempDir, "CPartialSample.h");
            var sourceFile = Path.Combine(tempDir, "CPartialSampleMethods.cpp");
            File.WriteAllText(headerFile, headerContent);
            File.WriteAllText(sourceFile, sourceContent);

            try
            {
                // Act
                var converter = new CppToCsStructuralConverter();
                converter.ConvertDirectory(tempDir, tempDir);

                // Assert
                var partialFile = Path.Combine(tempDir, "CPartialSampleMethods.cs");
                Assert.True(File.Exists(partialFile), $"Expected {partialFile} to exist");
                
                var generatedContent = File.ReadAllText(partialFile);
                
                // DistrStruct should NOT be a separate class
                Assert.DoesNotContain("internal class DistrStruct", generatedContent);
                
                // DistrStruct should be inside MethodOneInPartial body
                Assert.Contains("struct DistrStruct", generatedContent);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }
    }
}
