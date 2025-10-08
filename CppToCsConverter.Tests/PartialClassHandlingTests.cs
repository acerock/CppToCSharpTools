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
    /// Tests for TargetFileName-based partial class detection and generation.
    /// Verifies the integrated partial class approach using TargetFileName property.
    /// </summary>
    public class PartialClassHandlingTests
    {
        private readonly CppHeaderParser _headerParser;
        private readonly CppSourceParser _sourceParser;
        private readonly CppToCsStructuralConverter _converter;

        public PartialClassHandlingTests()
        {
            _headerParser = new CppHeaderParser();
            _sourceParser = new CppSourceParser();
            _converter = new CppToCsStructuralConverter();
        }

        [Fact]
        public void CppMethod_TargetFileName_DefaultsToEmptyString()
        {
            // Arrange & Act
            var method = new CppMethod { Name = "TestMethod" };

            // Assert
            Assert.Equal(string.Empty, method.TargetFileName);
        }

        [Fact]
        public void HeaderParser_InlineMethod_SetsTargetFileNameToHeaderName()
        {
            // Arrange - Header with inline method
            var headerContent = @"
class TestClass
{
public:
    int GetValue() { return 42; }
};";

            var tempFile = Path.GetTempFileName();
            var fileName = Path.GetFileNameWithoutExtension(tempFile);
            File.WriteAllText(tempFile, headerContent);

            try
            {
                // Act
                var classes = _headerParser.ParseHeaderFile(tempFile);

                // Assert
                var testClass = classes.FirstOrDefault(c => c.Name == "TestClass");
                Assert.NotNull(testClass);
                
                var inlineMethod = testClass.Methods.FirstOrDefault(m => m.Name == "GetValue");
                Assert.NotNull(inlineMethod);
                Assert.True(inlineMethod.HasInlineImplementation);
                Assert.Equal(fileName, inlineMethod.TargetFileName);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void HeaderParser_DeclarationOnlyMethod_DoesNotSetTargetFileName()
        {
            // Arrange - Header with declaration-only method
            var headerContent = @"
class TestClass
{
public:
    void DoSomething();
};";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, headerContent);

            try
            {
                // Act
                var classes = _headerParser.ParseHeaderFile(tempFile);

                // Assert
                var testClass = classes.FirstOrDefault(c => c.Name == "TestClass");
                Assert.NotNull(testClass);
                
                var method = testClass.Methods.FirstOrDefault(m => m.Name == "DoSomething");
                Assert.NotNull(method);
                Assert.False(method.HasInlineImplementation);
                Assert.Equal(string.Empty, method.TargetFileName);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void SourceParser_MethodImplementation_SetsTargetFileNameToSourceName()
        {
            // Arrange - Source file with method implementation
            var sourceContent = @"
void TestClass::DoSomething()
{
    // Implementation
}";

            var tempFile = Path.GetTempFileName();
            var fileName = Path.GetFileNameWithoutExtension(tempFile);
            File.WriteAllText(tempFile, sourceContent);

            try
            {
                // Act
                var (methods, _) = _sourceParser.ParseSourceFile(tempFile);

                // Assert
                var method = methods.FirstOrDefault(m => m.Name == "DoSomething");
                Assert.NotNull(method);
                Assert.Equal("TestClass", method.ClassName);
                Assert.Equal(fileName, method.TargetFileName);
                Assert.NotEmpty(method.ImplementationBody);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void CppClass_IsPartialClass_ReturnsFalseForNoMethods()
        {
            // Arrange
            var cppClass = new CppClass { Name = "EmptyClass" };

            // Act & Assert
            Assert.False(cppClass.IsPartialClass());
        }

        [Fact]
        public void CppClass_IsPartialClass_ReturnsFalseForSingleTargetFile()
        {
            // Arrange - Class with methods from single target file
            var cppClass = new CppClass 
            { 
                Name = "SingleFileClass",
                Methods = new List<CppMethod>
                {
                    new CppMethod { Name = "Method1", TargetFileName = "TestFile" },
                    new CppMethod { Name = "Method2", TargetFileName = "TestFile" }
                }
            };

            // Act & Assert
            Assert.False(cppClass.IsPartialClass());
        }

        [Fact]
        public void CppClass_IsPartialClass_ReturnsTrueForMultipleTargetFiles()
        {
            // Arrange - Class with methods from multiple target files
            var cppClass = new CppClass 
            { 
                Name = "MultiFileClass",
                Methods = new List<CppMethod>
                {
                    new CppMethod { Name = "InlineMethod", TargetFileName = "HeaderFile" },
                    new CppMethod { Name = "Method1", TargetFileName = "File1" },
                    new CppMethod { Name = "Method2", TargetFileName = "File2" }
                }
            };

            // Act & Assert
            Assert.True(cppClass.IsPartialClass());
        }

        [Fact]
        public void CppClass_IsPartialClass_IgnoresMethodsWithoutTargetFileName()
        {
            // Arrange - Mix of methods with and without target files
            var cppClass = new CppClass 
            { 
                Name = "MixedClass",
                Methods = new List<CppMethod>
                {
                    new CppMethod { Name = "NoTarget1", TargetFileName = "" },
                    new CppMethod { Name = "NoTarget2", TargetFileName = "" },
                    new CppMethod { Name = "WithTarget", TargetFileName = "File1" }
                }
            };

            // Act & Assert
            Assert.False(cppClass.IsPartialClass());
        }

        [Fact]
        public void CppClass_GetTargetFileNames_ReturnsUniqueFileNames()
        {
            // Arrange
            var cppClass = new CppClass 
            { 
                Name = "TestClass",
                Methods = new List<CppMethod>
                {
                    new CppMethod { Name = "Method1", TargetFileName = "File1" },
                    new CppMethod { Name = "Method2", TargetFileName = "File1" }, // Duplicate
                    new CppMethod { Name = "Method3", TargetFileName = "File2" },
                    new CppMethod { Name = "Method4", TargetFileName = "" } // Empty - should be ignored
                }
            };

            // Act
            var targetFiles = cppClass.GetTargetFileNames();

            // Assert
            Assert.True(targetFiles.Count == 2);
            Assert.Contains("File1", targetFiles);
            Assert.Contains("File2", targetFiles);
        }

        [Fact]
        public void CppClass_GetMethodsByTargetFile_GroupsMethodsCorrectly()
        {
            // Arrange
            var method1 = new CppMethod { Name = "Method1", TargetFileName = "File1" };
            var method2 = new CppMethod { Name = "Method2", TargetFileName = "File1" };
            var method3 = new CppMethod { Name = "Method3", TargetFileName = "File2" };
            
            var cppClass = new CppClass 
            { 
                Name = "TestClass",
                Methods = new List<CppMethod> { method1, method2, method3 }
            };

            // Act
            var methodsByTarget = cppClass.GetMethodsByTargetFile();

            // Assert
            Assert.Equal(2, methodsByTarget.Count);
            
            Assert.True(methodsByTarget.ContainsKey("File1"));
            Assert.Equal(2, methodsByTarget["File1"].Count);
            Assert.Contains(method1, methodsByTarget["File1"]);
            Assert.Contains(method2, methodsByTarget["File1"]);
            
            Assert.True(methodsByTarget.ContainsKey("File2"));
            Assert.Single(methodsByTarget["File2"]);
            Assert.Contains(method3, methodsByTarget["File2"]);
        }

        [Fact]
        public void StructuralConverter_GeneratesPartialClassForMultiTargetFiles()
        {
            // Arrange - Create test files
            var headerContent = @"
class MultiFileClass
{
private:
    int m_value1;
    int m_value2;
    
public:
    MultiFileClass();
    
    // Inline method
    int GetSum() { return m_value1 + m_value2; }
    
    // Methods to be implemented in source files
    void MethodFromFile1();
    void MethodFromFile2();
};";

            var source1Content = @"
void MultiFileClass::MethodFromFile1()
{
    m_value1 = 100;
}";

            var source2Content = @"
void MultiFileClass::MethodFromFile2()
{
    m_value2 = 200;
}";

            var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);

            var headerFile = Path.Combine(tempDir, "MultiFileClass.h");
            var source1File = Path.Combine(tempDir, "MultiFileClass.cpp");
            var source2File = Path.Combine(tempDir, "MultiFileClass_File2.cpp");

            File.WriteAllText(headerFile, headerContent);
            File.WriteAllText(source1File, source1Content);
            File.WriteAllText(source2File, source2Content);

            try
            {
                // Act
                _converter.ConvertDirectory(tempDir, tempDir);

                // Assert - Check main partial class file
                var mainOutputFile = Path.Combine(tempDir, "MultiFileClass.cs");
                Assert.True(File.Exists(mainOutputFile));
                
                var mainGeneratedContent = File.ReadAllText(mainOutputFile);
                
                // Should contain partial keyword
                Assert.Contains("partial class MultiFileClass", mainGeneratedContent);
                
                // Should contain inline method in main file
                Assert.Contains("GetSum()", mainGeneratedContent);
                Assert.Contains("return m_value1 + m_value2", mainGeneratedContent);
                
                // Should contain method from main source file (MultiFileClass.cpp)
                Assert.Contains("MethodFromFile1()", mainGeneratedContent);
                Assert.Contains("m_value1 = 100", mainGeneratedContent);
                
                // Assert - Check separate partial class file for File2
                var file2OutputFile = Path.Combine(tempDir, "MultiFileClass_File2.cs");
                Assert.True(File.Exists(file2OutputFile));
                
                var file2GeneratedContent = File.ReadAllText(file2OutputFile);
                
                // Should contain partial keyword in separate file
                Assert.Contains("partial class MultiFileClass", file2GeneratedContent);
                
                // Should contain method from MultiFileClass_File2.cpp
                Assert.Contains("MethodFromFile2()", file2GeneratedContent);
                Assert.Contains("m_value2 = 200", file2GeneratedContent);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Fact]
        public void StructuralConverter_GeneratesRegularClassForSingleFile()
        {
            // Arrange - Create test files with methods from single source
            var headerContent = @"
class SingleFileClass
{
private:
    int m_value;
    
public:
    SingleFileClass();
    void Method1();
    void Method2();
};";

            var sourceContent = @"
void SingleFileClass::Method1()
{
    m_value = 10;
}

void SingleFileClass::Method2()
{
    m_value = 20;
}";

            var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);

            var headerFile = Path.Combine(tempDir, "SingleFileClass.h");
            var sourceFile = Path.Combine(tempDir, "SingleFileClass.cpp");

            File.WriteAllText(headerFile, headerContent);
            File.WriteAllText(sourceFile, sourceContent);

            try
            {
                // Act
                _converter.ConvertDirectory(tempDir, tempDir);

                // Assert - No exceptions thrown means success
                
                var outputFile = Path.Combine(tempDir, "SingleFileClass.cs");
                Assert.True(File.Exists(outputFile));
                
                var generatedContent = File.ReadAllText(outputFile);
                
                // Should NOT contain partial keyword (methods from single source)
                Assert.DoesNotContain("partial class SingleFileClass", generatedContent);
                
                // Should contain regular class declaration
                Assert.Contains("class SingleFileClass", generatedContent);
                
                // Should contain implemented methods
                Assert.Contains("Method1()", generatedContent);
                Assert.Contains("Method2()", generatedContent);
                Assert.Contains("m_value = 10", generatedContent);
                Assert.Contains("m_value = 20", generatedContent);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Fact]
        public void StructuralConverter_GeneratesRegularClassForInlineOnlyMethods()
        {
            // Arrange - Class with only inline methods (all from header)
            var headerContent = @"
class InlineOnlyClass
{
private:
    int m_value;
    
public:
    InlineOnlyClass() : m_value(0) {}
    
    int GetValue() { return m_value; }
    void SetValue(int value) { m_value = value; }
    bool IsPositive() { return m_value > 0; }
};";

            var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);

            var headerFile = Path.Combine(tempDir, "InlineOnlyClass.h");
            File.WriteAllText(headerFile, headerContent);

            try
            {
                // Act
                _converter.ConvertDirectory(tempDir, tempDir);

                // Assert - No exceptions thrown means success
                
                var outputFile = Path.Combine(tempDir, "InlineOnlyClass.cs");
                Assert.True(File.Exists(outputFile));
                
                var generatedContent = File.ReadAllText(outputFile);
                
                // Should NOT contain partial keyword (all methods from same source - header)
                Assert.DoesNotContain("partial class InlineOnlyClass", generatedContent);
                
                // Should contain regular class declaration
                Assert.Contains("class InlineOnlyClass", generatedContent);
                
                // Should contain inline method implementations
                Assert.Contains("GetValue()", generatedContent);
                Assert.Contains("return m_value", generatedContent);
                Assert.Contains("SetValue(int value)", generatedContent);
                Assert.Contains("m_value = value", generatedContent);
                Assert.Contains("IsPositive()", generatedContent);
                Assert.Contains("return m_value > 0", generatedContent);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Fact]
        public void StructuralConverter_HandlesNonPartialClassWithStaticMembers()
        {
            // Arrange - Non-partial class with static member initialization (methods from same source file)
            var headerContent = @"
class NonPartialWithStatic
{
private:
    int m_instance;
    static const CString StaticArray[];
    
public:
    void HeaderMethod() { m_instance = 1; }
    void SourceMethod();
};";

            var sourceContent = @"
const CString NonPartialWithStatic::StaticArray[] = { ""value1"", ""value2"" };

void NonPartialWithStatic::SourceMethod()
{
    m_instance = 2;
}";

            var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);

            var headerFile = Path.Combine(tempDir, "NonPartialWithStatic.h");
            var sourceFile = Path.Combine(tempDir, "NonPartialWithStatic.cpp");

            File.WriteAllText(headerFile, headerContent);
            File.WriteAllText(sourceFile, sourceContent);

            try
            {
                // Act
                _converter.ConvertDirectory(tempDir, tempDir);

                // Assert - No exceptions thrown means success
                
                var outputFile = Path.Combine(tempDir, "NonPartialWithStatic.cs");
                Assert.True(File.Exists(outputFile));
                
                var generatedContent = File.ReadAllText(outputFile);
                
                // Should NOT contain partial keyword (methods from same source file)
                Assert.DoesNotContain("partial class NonPartialWithStatic", generatedContent);
                
                // Should contain regular class declaration
                Assert.Contains("class NonPartialWithStatic", generatedContent);
                
                // Should contain static array initialization
                Assert.Contains("CString[] StaticArray = {", generatedContent);
                Assert.Contains("\"value1\"", generatedContent);
                Assert.Contains("\"value2\"", generatedContent);
                
                // Should contain both header and source methods
                Assert.Contains("HeaderMethod()", generatedContent);
                Assert.Contains("m_instance = 1", generatedContent);
                Assert.Contains("SourceMethod()", generatedContent);
                Assert.Contains("m_instance = 2", generatedContent);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }
    }
}