using System;
using System.IO;
using System.Linq;
using CppToCsConverter.Core.Parsers;
using CppToCsConverter.Core.Core;
using Xunit;

namespace CppToCsConverter.Tests
{
    /// <summary>
    /// Tests for define statement parsing based on readme.md examples.
    /// Covers parsing #define statements from header files and associating them with classes.
    /// </summary>
    public class DefineStatementTests
    {
        private readonly CppHeaderParser _headerParser;

        public DefineStatementTests()
        {
            _headerParser = new CppHeaderParser();
        }

        [Fact]
        public void ParseHeaderFile_SimpleDefine_ShouldParseCorrectly()
        {
            // Arrange - Based on readme.md define example
            var headerContent = @"
#pragma once

// Here are some defines

// Comment for warning
#define WARNING 1
// Comment for stop
#define STOP 2
#define STOP_ALL 4

class CSample : public ISample
{
private:
    agrint m_value1;

public:
    void MethodOne(const CString& cParam1,
                   const bool &bParam2,
                   CString *pcParam3);    
}

// Some more defines
#define MY_DEFINE4 4
#define MY_DEFINE5 5
";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, headerContent);

            try
            {
                // Act
                var classes = _headerParser.ParseHeaderFile(tempFile);

                // Assert
                Assert.Single(classes);
                var cppClass = classes[0];
                Assert.Equal("CSample", cppClass.Name);
                
                // Should have collected 5 defines
                Assert.Equal(5, cppClass.HeaderDefines.Count);
                
                // Check first define with comment
                var warningDefine = cppClass.HeaderDefines.FirstOrDefault(d => d.Name == "WARNING");
                Assert.NotNull(warningDefine);
                Assert.Equal("1", warningDefine.Value);
                Assert.Equal("#define WARNING 1", warningDefine.FullDefinition);
                
                // Debug what comments we're getting
                Console.WriteLine($"WARNING define has {warningDefine.PrecedingComments.Count} comments:");
                foreach (var comment in warningDefine.PrecedingComments)
                {
                    Console.WriteLine($"  - '{comment}'");
                }
                
                // The comment collection might include multiple comments, let's verify the right one is there
                Assert.Contains("// Comment for warning", warningDefine.PrecedingComments);
                
                // Check define without comment
                var stopAllDefine = cppClass.HeaderDefines.FirstOrDefault(d => d.Name == "STOP_ALL");
                Assert.NotNull(stopAllDefine);
                Assert.Equal("4", stopAllDefine.Value);
                Assert.Equal("#define STOP_ALL 4", stopAllDefine.FullDefinition);
                
                // Check defines after class
                var myDefine4 = cppClass.HeaderDefines.FirstOrDefault(d => d.Name == "MY_DEFINE4");
                Assert.NotNull(myDefine4);
                Assert.Equal("4", myDefine4.Value);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseHeaderFile_DefineWithoutValue_ShouldIgnoreValuelessDefines()
        {
            // Arrange
            var headerContent = @"
#define FEATURE_ENABLED
#define DEBUG_MODE 1
#define HEADER_GUARD_H_

class TestClass
{
public:
    void Method();
};
";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, headerContent);

            try
            {
                // Act
                var classes = _headerParser.ParseHeaderFile(tempFile);

                // Assert
                Assert.Single(classes);
                var cppClass = classes[0];
                // Only defines with values should be collected (per readme.md specification)
                Assert.Single(cppClass.HeaderDefines);
                
                // Valueless defines should be ignored
                var featureDefine = cppClass.HeaderDefines.FirstOrDefault(d => d.Name == "FEATURE_ENABLED");
                Assert.Null(featureDefine);
                
                var headerGuardDefine = cppClass.HeaderDefines.FirstOrDefault(d => d.Name == "HEADER_GUARD_H_");
                Assert.Null(headerGuardDefine);
                
                // Only defines with values should be present
                var debugDefine = cppClass.HeaderDefines.FirstOrDefault(d => d.Name == "DEBUG_MODE");
                Assert.NotNull(debugDefine);
                Assert.Equal("1", debugDefine.Value);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseHeaderFile_NoDefines_ShouldReturnEmptyList()
        {
            // Arrange
            var headerContent = @"
class TestClass
{
public:
    void Method();
};
";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, headerContent);

            try
            {
                // Act
                var classes = _headerParser.ParseHeaderFile(tempFile);

                // Assert
                Assert.Single(classes);
                var cppClass = classes[0];
                Assert.Empty(cppClass.HeaderDefines);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseSourceFile_SimpleDefine_ShouldParseCorrectly()
        {
            // Arrange - Based on readme.md source define example
            var sourceContent = @"
/* DEFINES IN CPP*/
// Also cpp files can have defines
#define CPP_DEFINE 10
#define CPP_DEFINE2 20 
// Comment for cpp define 3
#define CPP_DEFINE3 30 

void CSample::MethodOne(const CString& cParam1,
                   const bool &bParam2,
                   CString *pcParam3)
{
    // Implementation of MethodOne
}

#define CPP_DEFINE4 40
";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, sourceContent);

            try
            {
                // Act
                var sourceParser = new CppSourceParser();
                var (methods, staticInits, defines) = sourceParser.ParseSourceFileWithDefines(tempFile);

                // Assert
                Assert.Equal(4, defines.Count);
                
                // Check first define
                var cppDefine = defines.FirstOrDefault(d => d.Name == "CPP_DEFINE");
                Assert.NotNull(cppDefine);
                Assert.Equal("10", cppDefine.Value);
                Assert.Equal("#define CPP_DEFINE 10", cppDefine.FullDefinition);
                
                // Check define with comment
                var cppDefine3 = defines.FirstOrDefault(d => d.Name == "CPP_DEFINE3");
                Assert.NotNull(cppDefine3);
                Assert.Equal("30", cppDefine3.Value);
                Assert.Contains("// Comment for cpp define 3", cppDefine3.PrecedingComments);
                
                // Check define after method
                var cppDefine4 = defines.FirstOrDefault(d => d.Name == "CPP_DEFINE4");
                Assert.NotNull(cppDefine4);
                Assert.Equal("40", cppDefine4.Value);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void FullPipeline_DefinesFromHeaderAndSource_ShouldGenerateCorrectly()
        {
            // Arrange - Based on readme.md example with defines in both header and source
            var headerContent = @"
#pragma once

// Here are some defines

// Comment for warning
#define WARNING 1
// Comment for stop
#define STOP 2
#define STOP_ALL 4

class CSample : public ISample
{
private:
    agrint m_value1;

public:
    void MethodOne(const CString& cParam1,
                   const bool &bParam2,
                   CString *pcParam3);    
}

// Some more defines
#define MY_DEFINE4 4
#define MY_DEFINE5 5
";

            var sourceContent = @"
/* DEFINES IN CPP*/
// Also cpp files can have defines
#define CPP_DEFINE 10
#define CPP_DEFINE2 20 
// Comment for cpp define 3
#define CPP_DEFINE3 30 

void CSample::MethodOne(const CString& cParam1,
                   const bool &bParam2,
                   CString *pcParam3)
{
    // Implementation of MethodOne
}

#define CPP_DEFINE4 40
";

            // Create temporary directory for test
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            
            var headerFile = Path.Combine(tempDir, "CSample.h");
            var sourceFile = Path.Combine(tempDir, "CSample.cpp");
            var outputDir = Path.Combine(tempDir, "output");
            
            File.WriteAllText(headerFile, headerContent);
            File.WriteAllText(sourceFile, sourceContent);

            try
            {
                // Act - Use the full converter pipeline
                var converter = new CppToCsStructuralConverter();
                converter.ConvertFiles(new[] { headerFile }, new[] { sourceFile }, outputDir);

                // Assert - Check generated C# file
                var outputFile = Path.Combine(outputDir, "CSample.cs");
                Assert.True(File.Exists(outputFile), $"Output file should exist: {outputFile}");
                
                var generatedContent = File.ReadAllText(outputFile);
                Console.WriteLine("Generated C# content:");
                Console.WriteLine(generatedContent);
                
                // Should contain header defines
                Assert.Contains("#define WARNING 1", generatedContent);
                Assert.Contains("#define STOP 2", generatedContent);
                Assert.Contains("#define STOP_ALL 4", generatedContent);
                Assert.Contains("#define MY_DEFINE4 4", generatedContent);
                Assert.Contains("#define MY_DEFINE5 5", generatedContent);
                
                // Should contain source defines
                Assert.Contains("#define CPP_DEFINE 10", generatedContent);
                Assert.Contains("#define CPP_DEFINE2 20", generatedContent);
                Assert.Contains("#define CPP_DEFINE3 30", generatedContent);
                Assert.Contains("#define CPP_DEFINE4 40", generatedContent);
                
                // Should contain comments
                Assert.Contains("// Comment for warning", generatedContent);
                Assert.Contains("// Comment for cpp define 3", generatedContent);
                
                // Should contain the class and method
                Assert.Contains("internal class CSample", generatedContent);
                Assert.Contains("public void MethodOne", generatedContent);
            }
            finally
            {
                // Clean up
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }
    }
}