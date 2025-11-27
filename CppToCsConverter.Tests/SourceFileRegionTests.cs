using System.IO;
using System.Linq;
using Xunit;
using CppToCsConverter.Core.Parsers;
using CppToCsConverter.Core.Core;

namespace CppToCsConverter.Tests
{
    /// <summary>
    /// Tests for region handling in source (.cpp) files.
    /// Based on readme.md requirement: "we only want to recreate regions from the .cpp files"
    /// Regions in .cpp should be preserved as #region/#endregion in .cs
    /// </summary>
    public class SourceFileRegionTests
    {
        private readonly CppSourceParser _sourceParser;
        private readonly CppHeaderParser _headerParser;
        private readonly CppToCsStructuralConverter _converter;

        public SourceFileRegionTests()
        {
            _sourceParser = new CppSourceParser();
            _headerParser = new CppHeaderParser();
            _converter = new CppToCsStructuralConverter();
        }

        [Fact]
        public void ParseSourceFile_WithRegionAroundConstructor_ShouldCaptureRegionStartAndEnd()
        {
            // Arrange
            var sourceContent = @"
#include ""StdAfx.h""

#region Constructors

CPartialSample::CPartialSample()
{
    m_value1 = 0;
}

CPartialSample::~CPartialSample()
{
    // Cleanup code here
}

#endregion
";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, sourceContent);

            try
            {
                // Act
                var (methods, staticInits) = _sourceParser.ParseSourceFile(tempFile);

                // Debug output
                System.Console.WriteLine("=== PARSED METHODS ===");
                foreach (var m in methods)
                {
                    System.Console.WriteLine($"Method: {m.Name,-20} RegionStart: [{m.SourceRegionStart}]  RegionEnd: [{m.SourceRegionEnd}]");
                }
                System.Console.WriteLine("======================");

                // Assert
                Assert.Equal(2, methods.Count);
                
                // At least one method should have captured the region
                var hasRegionStart = methods.Any(m => !string.IsNullOrEmpty(m.SourceRegionStart) && m.SourceRegionStart.Contains("Constructors"));
                var hasRegionEnd = methods.Any(m => !string.IsNullOrEmpty(m.SourceRegionEnd));
                
                Assert.True(hasRegionStart, "Expected at least one method to have SourceRegionStart with 'Constructors'");
                Assert.True(hasRegionEnd, "Expected at least one method to have SourceRegionEnd");
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseSourceFile_WithRegionAroundMethods_ShouldCaptureRegionDetails()
        {
            // Arrange
            var sourceContent = @"
#include ""StdAfx.h""

void CSample::MethodOne()
{
    m_value = 1;
}

#pragma region My Nice Region

void CSample::MethodTwo()
{
    m_value = 2;
}

void CSample::MethodThree()
{
    m_value = 3;
}

#pragma endregion // My Nice Region
";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, sourceContent);

            try
            {
                // Act
                var (methods, staticInits) = _sourceParser.ParseSourceFile(tempFile);

                // Assert
                Assert.Equal(3, methods.Count);
                
                var method1 = methods[0];
                Assert.Equal("MethodOne", method1.Name);
                Assert.Empty(method1.SourceRegionStart);
                Assert.Empty(method1.SourceRegionEnd);
                
                // At least one of the methods in the region should have captured the region markers
                var methodsInRegion = methods.Skip(1).ToList(); // method2 and method3
                var hasRegionStart = methodsInRegion.Any(m => !string.IsNullOrEmpty(m.SourceRegionStart) && m.SourceRegionStart.Contains("My Nice Region"));
                var hasRegionEnd = methodsInRegion.Any(m => !string.IsNullOrEmpty(m.SourceRegionEnd) && m.SourceRegionEnd.Contains("My Nice Region"));
                
                Assert.True(hasRegionStart, "Expected at least one method in region to have SourceRegionStart");
                Assert.True(hasRegionEnd, "Expected at least one method in region to have SourceRegionEnd");
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ConvertClassWithSourceRegions_ShouldWriteRegionMarkersToCS()
        {
            // Arrange - Create header file
            var headerContent = @"
#pragma once

class CSample
{
public:
    CSample();
    ~CSample();
    void MethodOne();
    void MethodTwo();
};
";

            // Arrange - Create source file with regions
            var sourceContent = @"
#include ""StdAfx.h""
#include ""CSample.h""

#region Constructors

CSample::CSample()
{
    m_value = 0;
}

CSample::~CSample()
{
    // Cleanup
}

#endregion

#region Methods

void CSample::MethodOne()
{
    m_value = 1;
}

void CSample::MethodTwo()
{
    m_value = 2;
}

#endregion // Methods
";

            var tempDir = Path.Combine(Directory.GetCurrentDirectory(), "Work", "Temp", "RegionTest1");
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
            Directory.CreateDirectory(tempDir);

            var headerFile = Path.Combine(tempDir, "CSample.h");
            var sourceFile = Path.Combine(tempDir, "CSample.cpp");
            var outputDir = Path.Combine(tempDir, "Output");

            File.WriteAllText(headerFile, headerContent);
            File.WriteAllText(sourceFile, sourceContent);

            try
            {
                // Act
                _converter.ConvertFiles(
                    new[] { headerFile },
                    new[] { sourceFile },
                    outputDir,
                    tempDir
                );

                // Assert
                var csFile = Path.Combine(outputDir, "CSample.cs");
                Assert.True(File.Exists(csFile), $"Expected output file not found: {csFile}");

                var generatedContent = File.ReadAllText(csFile);

                // Should have #region Constructors
                Assert.Contains("#region Constructors", generatedContent);
                
                // Constructor should appear after region start
                var regionConstructorsIndex = generatedContent.IndexOf("#region Constructors");
                var constructorIndex = generatedContent.IndexOf("public CSample()");
                Assert.True(constructorIndex > regionConstructorsIndex, 
                    "Constructor should appear after #region Constructors");

                // Should have #endregion after destructor but before Methods region
                var destructorIndex = generatedContent.IndexOf("public ~CSample()");
                var firstEndRegionIndex = generatedContent.IndexOf("#endregion", destructorIndex);
                Assert.True(firstEndRegionIndex > destructorIndex, 
                    "First #endregion should appear after destructor");

                // Should have #region Methods
                Assert.Contains("#region Methods", generatedContent);
                
                // MethodOne should appear after #region Methods
                var regionMethodsIndex = generatedContent.IndexOf("#region Methods");
                var method1Index = generatedContent.IndexOf("public void MethodOne()");
                Assert.True(method1Index > regionMethodsIndex,
                    "MethodOne should appear after #region Methods");

                // Should have #endregion // Methods after MethodTwo
                Assert.Contains("#endregion // Methods", generatedContent);
                var method2Index = generatedContent.IndexOf("public void MethodTwo()");
                var secondEndRegionIndex = generatedContent.IndexOf("#endregion // Methods");
                Assert.True(secondEndRegionIndex > method2Index,
                    "#endregion // Methods should appear after MethodTwo");
            }
            finally
            {
                // Keep temp files for debugging - don't delete
                // if (Directory.Exists(tempDir))
                //     Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void ConvertPartialClass_WithSourceRegions_ShouldWriteRegionsToPartialFile()
        {
            // Arrange - Create header file
            var headerContent = @"
#pragma once

class CPartialSample
{
private:
    agrint m_value1;

public:
    CPartialSample();
    ~CPartialSample();
    void MethodOne();
    void GetRelValue();
};
";

            // Arrange - Main source file with regions
            var mainSourceContent = @"
#include ""StdAfx.h""
#include ""CPartialSample.h""

#region Constructors

CPartialSample::CPartialSample()
{
    m_value1 = 0;
}

CPartialSample::~CPartialSample()
{
    // Cleanup code here
}

#endregion

void CPartialSample::MethodOne()
{
    m_value1 = 1;
}
";

            // Arrange - Partial source file with region
            var partialSourceContent = @"
#include ""StdAfx.h""
#include ""CPartialSample.h""

#region Get Methods

void CPartialSample::GetRelValue()
{
    return m_value1;
}

#endregion
";

            var tempDir = Path.Combine(Directory.GetCurrentDirectory(), "Work", "Temp", "RegionTest2");
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
            Directory.CreateDirectory(tempDir);

            var headerFile = Path.Combine(tempDir, "CPartialSample.h");
            var mainSourceFile = Path.Combine(tempDir, "CPartialSample.cpp");
            var partialSourceFile = Path.Combine(tempDir, "CPartialSampleGet.cpp");
            var outputDir = Path.Combine(tempDir, "Output");

            File.WriteAllText(headerFile, headerContent);
            File.WriteAllText(mainSourceFile, mainSourceContent);
            File.WriteAllText(partialSourceFile, partialSourceContent);

            try
            {
                // Act
                _converter.ConvertFiles(
                    new[] { headerFile },
                    new[] { mainSourceFile, partialSourceFile },
                    outputDir,
                    tempDir
                );

                // Assert - Check main file
                var mainCsFile = Path.Combine(outputDir, "CPartialSample.cs");
                Assert.True(File.Exists(mainCsFile));

                var mainContent = File.ReadAllText(mainCsFile);
                Assert.Contains("#region Constructors", mainContent);
                Assert.Contains("#endregion", mainContent);

                // Assert - Check partial file
                var partialCsFile = Path.Combine(outputDir, "CPartialSampleGet.cs");
                Assert.True(File.Exists(partialCsFile));

                var partialContent = File.ReadAllText(partialCsFile);
                Assert.Contains("#region Get Methods", partialContent);
                Assert.Contains("#endregion", partialContent);
                Assert.Contains("partial class CPartialSample", partialContent);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }
    }
}
