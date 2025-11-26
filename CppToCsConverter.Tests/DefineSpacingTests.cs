using System;
using System.IO;
using System.Linq;
using Xunit;
using CppToCsConverter.Core;

namespace CppToCsConverter.Tests
{
    public class DefineSpacingTests
    {
        [Fact]
        public void PublicDefines_WithoutPrecedingComments_ShouldBeOnConsecutiveLines()
        {
            // Arrange
            string headerContent = @"
#pragma once

// Some define
#define IN_INTERFACE_DEF01 1
#define IN_INTERFACE_DEF02 2 // Another define

class __declspec(dllexport) ISample
{
public:
    virtual void MethodOne() = 0;
};
";

            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            
            try
            {
                File.WriteAllText(Path.Combine(tempDir, "ISample.h"), headerContent);

                // Act
                var api = new CppToCsConverterApi();
                api.ConvertDirectory(tempDir, tempDir);

                // Assert
                string definesFile = Path.Combine(tempDir, "SampleDefines.cs");
                Assert.True(File.Exists(definesFile), "SampleDefines.cs should be generated");

                string generatedContent = File.ReadAllText(definesFile);

                // The two defines should be on consecutive lines with no blank line between them
                Assert.Contains("// Some define", generatedContent);
                Assert.Contains("public const int IN_INTERFACE_DEF01 = 1;", generatedContent);
                Assert.Contains("public const int IN_INTERFACE_DEF02 = 2; // Another define", generatedContent);

                // Verify they are on consecutive lines (no blank line between)
                var lines = generatedContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                int def01Index = Array.FindIndex(lines, l => l.Contains("IN_INTERFACE_DEF01"));
                int def02Index = Array.FindIndex(lines, l => l.Contains("IN_INTERFACE_DEF02"));

                Assert.True(def01Index >= 0, "IN_INTERFACE_DEF01 should be found");
                Assert.True(def02Index >= 0, "IN_INTERFACE_DEF02 should be found");
                // Defines without preceding comments should be on consecutive lines
                Assert.Equal(def01Index + 1, def02Index);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void PublicDefines_WithPrecedingComments_ShouldHaveBlankLineBefore()
        {
            // Arrange
            string headerContent = @"
#pragma once

// First define
#define DEFINE_ONE 1

// Second define group
#define DEFINE_TWO 2
#define DEFINE_THREE 3

class __declspec(dllexport) ISample
{
public:
    virtual void MethodOne() = 0;
};
";

            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            
            try
            {
                File.WriteAllText(Path.Combine(tempDir, "ISample.h"), headerContent);

                // Act
                var api = new CppToCsConverterApi();
                api.ConvertDirectory(tempDir, tempDir);

                // Assert
                string definesFile = Path.Combine(tempDir, "SampleDefines.cs");
                string generatedContent = File.ReadAllText(definesFile);

                var lines = generatedContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                
                // Find indices
                int def01Index = Array.FindIndex(lines, l => l.Contains("DEFINE_ONE"));
                int def02CommentIndex = Array.FindIndex(lines, l => l.Contains("// Second define group"));
                int def02Index = Array.FindIndex(lines, l => l.Contains("DEFINE_TWO"));
                int def03Index = Array.FindIndex(lines, l => l.Contains("DEFINE_THREE"));

                // DEFINE_TWO should have a blank line before its comment (because it has preceding comments)
                Assert.True(string.IsNullOrWhiteSpace(lines[def02CommentIndex - 1]), 
                    "There should be a blank line before a define's preceding comment");

                // DEFINE_THREE should be on the line immediately after DEFINE_TWO (no preceding comments)
                Assert.Equal(def02Index + 1, def03Index);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void ClassDefines_WithoutPrecedingComments_ShouldBeOnConsecutiveLines()
        {
            // Arrange
            string headerContent = @"
#pragma once

#define MY_DEFINE 1
#define MY_DEFINE2 2
// Comment for define 3
#define MY_DEFINE3 3

class CSample
{
public:
    void MethodOne();
};
";

            string sourceContent = @"
#include ""CSample.h""

void CSample::MethodOne()
{
    // Implementation
}
";

            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            
            try
            {
                File.WriteAllText(Path.Combine(tempDir, "CSample.h"), headerContent);
                File.WriteAllText(Path.Combine(tempDir, "CSample.cpp"), sourceContent);

                // Act
                var api = new CppToCsConverterApi();
                api.ConvertDirectory(tempDir, tempDir);

                // Assert
                string csFile = Path.Combine(tempDir, "CSample.cs");
                string generatedContent = File.ReadAllText(csFile);

                var lines = generatedContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                
                int def01Index = Array.FindIndex(lines, l => l.Contains("MY_DEFINE = 1"));
                int def02Index = Array.FindIndex(lines, l => l.Contains("MY_DEFINE2 = 2"));
                int def03CommentIndex = Array.FindIndex(lines, l => l.Contains("// Comment for define 3"));

                // MY_DEFINE and MY_DEFINE2 should be consecutive (no preceding comments on MY_DEFINE2)
                Assert.Equal(def01Index + 1, def02Index);

                // MY_DEFINE3 should have a blank line before its comment
                Assert.True(string.IsNullOrWhiteSpace(lines[def03CommentIndex - 1]), 
                    "There should be a blank line before a define's preceding comment");
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void MixedHeaderAndSourceDefines_ShouldMaintainCorrectSpacing()
        {
            // Arrange
            string headerContent = @"
#pragma once

// Top defines
#define MY_DEFINE 1
#define MY_DEFINE2 2
// Comment for define 3
#define MY_DEFINE3 3

class CSample
{
public:
    void MethodOne();
};
";

            string sourceContent = @"
#include ""CSample.h""

/* DEFINES IN CPP*/
// Also cpp files can have defines
#define CPP_DEFINE 10
#define CPP_DEFINE2 20
// Comment for cpp define 3
#define CPP_DEFINE3 30

void CSample::MethodOne()
{
    // Implementation
}
";

            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            
            try
            {
                File.WriteAllText(Path.Combine(tempDir, "CSample.h"), headerContent);
                File.WriteAllText(Path.Combine(tempDir, "CSample.cpp"), sourceContent);

                // Act
                var api = new CppToCsConverterApi();
                api.ConvertDirectory(tempDir, tempDir);

                // Assert
                string csFile = Path.Combine(tempDir, "CSample.cs");
                string generatedContent = File.ReadAllText(csFile);

                var lines = generatedContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                
                // Check header defines
                int myDef1Index = Array.FindIndex(lines, l => l.Contains("MY_DEFINE = 1"));
                int myDef2Index = Array.FindIndex(lines, l => l.Contains("MY_DEFINE2 = 2"));
                // Header defines without comments should be consecutive
                Assert.Equal(myDef1Index + 1, myDef2Index);

                // Check source defines
                int cppDef1Index = Array.FindIndex(lines, l => l.Contains("CPP_DEFINE = 10"));
                int cppDef2Index = Array.FindIndex(lines, l => l.Contains("CPP_DEFINE2 = 20"));
                // Source defines without comments should be consecutive
                Assert.Equal(cppDef1Index + 1, cppDef2Index);

                // Verify source defines section has blank line before its comment block
                int cppDefinesCommentIndex = Array.FindIndex(lines, l => l.Contains("/* DEFINES IN CPP*/"));
                Assert.True(string.IsNullOrWhiteSpace(lines[cppDefinesCommentIndex - 1]), 
                    "There should be a blank line before source defines section");
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }
    }
}
