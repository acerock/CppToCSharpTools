using System.IO;
using System.Linq;
using System.Collections.Generic;
using Xunit;
using CppToCsConverter.Core.Models;
using CppToCsConverter.Core.Parsers;
using CppToCsConverter.Core.Core;

namespace CppToCsConverter.Tests
{
    /// <summary>
    /// Tests for method ordering in C# generation, especially for partial classes.
    /// Verifies that methods appear in the same order as defined in source .cpp files.
    /// </summary>
    public class MethodOrderingTests
    {
        private readonly CppHeaderParser _headerParser;
        private readonly CppSourceParser _sourceParser;
        private readonly CppToCsStructuralConverter _converter;

        public MethodOrderingTests()
        {
            _headerParser = new CppHeaderParser();
            _sourceParser = new CppSourceParser();
            _converter = new CppToCsStructuralConverter();
        }

        [Fact]
        public void PartialClass_MethodOrder_ShouldFollowSourceFileOrder()
        {
            // Arrange - Create header file with method declarations
            var headerContent = @"
#pragma once

class TestClass
{
public:
    void FirstMethod();
    void SecondMethod(); 
    void ThirdMethod();
    void FourthMethod();
    void FifthMethod();
};";

            // Create two source files with methods in different orders
            var sourceFile1Content = @"
void TestClass::ThirdMethod()
{
    // Third method implementation
}

void TestClass::FirstMethod()
{
    // First method implementation
}";

            var sourceFile2Content = @"
void TestClass::FifthMethod()
{
    // Fifth method implementation
}

void TestClass::SecondMethod()
{
    // Second method implementation
}

void TestClass::FourthMethod()
{
    // Fourth method implementation
}";

            var tempDir = Path.GetTempPath();
            var headerFile = Path.Combine(tempDir, "TestClass.h");
            var sourceFile1 = Path.Combine(tempDir, "TestClass.cpp");
            var sourceFile2 = Path.Combine(tempDir, "TestClassExtra.cpp");
            var tempOutputDir = Path.Combine(tempDir, "OutputTest");

            try
            {
                File.WriteAllText(headerFile, headerContent);
                File.WriteAllText(sourceFile1, sourceFile1Content);
                File.WriteAllText(sourceFile2, sourceFile2Content);
                Directory.CreateDirectory(tempOutputDir);

                // Act
                _converter.ConvertFiles(new[] { headerFile }, new[] { sourceFile1, sourceFile2 }, tempOutputDir);

                // Assert - Check that files were generated
                
                var mainClassFile = Path.Combine(tempOutputDir, "TestClass.cs");
                var extraClassFile = Path.Combine(tempOutputDir, "TestClassExtra.cs");
                
                Assert.True(File.Exists(mainClassFile));
                Assert.True(File.Exists(extraClassFile));

                // Read generated files
                var mainClassContent = File.ReadAllText(mainClassFile);
                var extraClassContent = File.ReadAllText(extraClassFile);

                // Verify method order in main file (TestClass.cpp order: ThirdMethod, FirstMethod)
                var mainMethodIndexThird = mainClassContent.IndexOf("void ThirdMethod()");
                var mainMethodIndexFirst = mainClassContent.IndexOf("void FirstMethod()");
                
                Assert.True(mainMethodIndexThird >= 0, "ThirdMethod should be present in main file");
                Assert.True(mainMethodIndexFirst >= 0, "FirstMethod should be present in main file");
                Assert.True(mainMethodIndexThird < mainMethodIndexFirst, 
                    $"ThirdMethod should appear before FirstMethod in main file. ThirdMethod at {mainMethodIndexThird}, FirstMethod at {mainMethodIndexFirst}");

                // Verify method order in extra file (TestClassExtra.cpp order: FifthMethod, SecondMethod, FourthMethod)
                var extraMethodIndexFifth = extraClassContent.IndexOf("void FifthMethod()");
                var extraMethodIndexSecond = extraClassContent.IndexOf("void SecondMethod()");
                var extraMethodIndexFourth = extraClassContent.IndexOf("void FourthMethod()");
                
                Assert.True(extraMethodIndexFifth >= 0, "FifthMethod should be present in extra file");
                Assert.True(extraMethodIndexSecond >= 0, "SecondMethod should be present in extra file");
                Assert.True(extraMethodIndexFourth >= 0, "FourthMethod should be present in extra file");
                
                Assert.True(extraMethodIndexFifth < extraMethodIndexSecond, 
                    $"FifthMethod should appear before SecondMethod. FifthMethod at {extraMethodIndexFifth}, SecondMethod at {extraMethodIndexSecond}");
                Assert.True(extraMethodIndexSecond < extraMethodIndexFourth, 
                    $"SecondMethod should appear before FourthMethod. SecondMethod at {extraMethodIndexSecond}, FourthMethod at {extraMethodIndexFourth}");

                // Debug output
                System.Console.WriteLine("=== MAIN CLASS CONTENT ===");
                System.Console.WriteLine(mainClassContent);
                System.Console.WriteLine("\n=== EXTRA CLASS CONTENT ===");
                System.Console.WriteLine(extraClassContent);
            }
            finally
            {
                // Cleanup
                if (File.Exists(headerFile)) File.Delete(headerFile);
                if (File.Exists(sourceFile1)) File.Delete(sourceFile1);
                if (File.Exists(sourceFile2)) File.Delete(sourceFile2);
                if (Directory.Exists(tempOutputDir)) Directory.Delete(tempOutputDir, true);
            }
        }

        [Fact]
        public void SingleClass_MethodOrder_ShouldFollowSourceFileOrder()
        {
            // Arrange - Create header file with method declarations
            var headerContent = @"
#pragma once

class SingleClass
{
public:
    void FirstMethod();
    void SecondMethod(); 
    void ThirdMethod();
};";

            // Create source file with methods in specific order
            var sourceContent = @"
void SingleClass::ThirdMethod()
{
    // Third method implementation
}

void SingleClass::FirstMethod()
{
    // First method implementation
}

void SingleClass::SecondMethod()
{
    // Second method implementation
}";

            var tempDir = Path.GetTempPath();
            var headerFile = Path.Combine(tempDir, "SingleClass.h");
            var sourceFile = Path.Combine(tempDir, "SingleClass.cpp");
            var tempOutputDir = Path.Combine(tempDir, "OutputSingle");

            try
            {
                File.WriteAllText(headerFile, headerContent);
                File.WriteAllText(sourceFile, sourceContent);
                Directory.CreateDirectory(tempOutputDir);

                // Act
                _converter.ConvertFiles(new[] { headerFile }, new[] { sourceFile }, tempOutputDir);

                // Assert
                
                var classFile = Path.Combine(tempOutputDir, "SingleClass.cs");
                Assert.True(File.Exists(classFile));

                var classContent = File.ReadAllText(classFile);

                // Verify method order (source order: ThirdMethod, FirstMethod, SecondMethod)
                var methodIndexThird = classContent.IndexOf("void ThirdMethod()");
                var methodIndexFirst = classContent.IndexOf("void FirstMethod()");
                var methodIndexSecond = classContent.IndexOf("void SecondMethod()");
                
                Assert.True(methodIndexThird >= 0, "ThirdMethod should be present");
                Assert.True(methodIndexFirst >= 0, "FirstMethod should be present");
                Assert.True(methodIndexSecond >= 0, "SecondMethod should be present");
                
                Assert.True(methodIndexThird < methodIndexFirst, 
                    $"ThirdMethod should appear before FirstMethod. ThirdMethod at {methodIndexThird}, FirstMethod at {methodIndexFirst}");
                Assert.True(methodIndexFirst < methodIndexSecond, 
                    $"FirstMethod should appear before SecondMethod. FirstMethod at {methodIndexFirst}, SecondMethod at {methodIndexSecond}");

                // Debug output
                System.Console.WriteLine("=== SINGLE CLASS CONTENT ===");
                System.Console.WriteLine(classContent);
            }
            finally
            {
                // Cleanup
                if (File.Exists(headerFile)) File.Delete(headerFile);
                if (File.Exists(sourceFile)) File.Delete(sourceFile);
                if (Directory.Exists(tempOutputDir)) Directory.Delete(tempOutputDir, true);
            }
        }

        [Fact]
        public void MethodOrderIndex_ShouldBeSetCorrectlyByParser()
        {
            // Arrange - Create source file with methods in specific order
            var sourceContent = @"
void TestClass::ThirdMethod()
{
    // Third method implementation
}

void TestClass::FirstMethod()
{
    // First method implementation
}

void TestClass::SecondMethod()
{
    // Second method implementation
}";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, sourceContent);

            try
            {
                // Act
                var (methods, _) = _sourceParser.ParseSourceFile(tempFile);

                // Assert - Verify OrderIndex is set according to source file order
                var sortedMethods = methods.OrderBy(m => m.OrderIndex).ToList();
                
                Assert.Equal(3, methods.Count);
                // OrderIndex now uses character position, so check relative ordering
                Assert.Equal("ThirdMethod", sortedMethods[0].Name);
                Assert.Equal("FirstMethod", sortedMethods[1].Name);
                Assert.Equal("SecondMethod", sortedMethods[2].Name);
                
                // Verify OrderIndex increases in file order
                Assert.True(sortedMethods[0].OrderIndex < sortedMethods[1].OrderIndex,
                    $"ThirdMethod OrderIndex ({sortedMethods[0].OrderIndex}) should be less than FirstMethod OrderIndex ({sortedMethods[1].OrderIndex})");
                Assert.True(sortedMethods[1].OrderIndex < sortedMethods[2].OrderIndex,
                    $"FirstMethod OrderIndex ({sortedMethods[1].OrderIndex}) should be less than SecondMethod OrderIndex ({sortedMethods[2].OrderIndex})");

                // Debug output
                System.Console.WriteLine("=== PARSED METHODS ORDER ===");
                foreach (var method in sortedMethods)
                {
                    System.Console.WriteLine($"{method.OrderIndex}: {method.Name}");
                }
            }
            finally
            {
                File.Delete(tempFile);
            }
        }
    }
}