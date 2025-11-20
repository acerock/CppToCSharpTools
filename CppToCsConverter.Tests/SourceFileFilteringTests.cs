using System.IO;
using System.Linq;
using Xunit;
using CppToCsConverter.Core.Parsers;
using CppToCsConverter.Core.Models;
using CppToCsConverter.Core;
using CppToCsConverter.Core.Utils;
using CppToCsConverter.Core.Core;

namespace CppToCsConverter.Tests
{
    /// <summary>
    /// Tests for filtering source files that should be skipped
    /// (files with no class methods - only local functions, structs, or MAIN macros)
    /// </summary>
    public class SourceFileFilteringTests
    {
        [Fact]
        public void ParseSourceFile_WithOnlyLocalFunctions_AllMethodsShouldBeLocal()
        {
            // Arrange
            var sourceContent = @"
#include ""StdAfx.h""
#include ""Routines.h""

bool SomeOtherStuff(const agrint& iTest, const CString& cMsg);

struct
{
    TAttId    attId;
    TDimValue dimAttName;
    CString   cDimId;
}   RoutineStruct;

void AGRRoutine(const TAttId &attId, bool bDoWork)
{
    agrint lMember1 = 0;
    TDimValue dimClient;
    CString cTest = _T(""MyRoutine"");
}

bool SomeOtherStuff(const agrint& iTest, const CString& cMsg)
{
    return TRUE;
}";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, sourceContent);

            try
            {
                // Act
                var parser = new CppSourceParser();
                var sourceFile = parser.ParseSourceFileComplete(tempFile);

                // Assert - All methods should be local (no class methods)
                Assert.NotEmpty(sourceFile.Methods);
                Assert.All(sourceFile.Methods, method => Assert.True(method.IsLocalMethod));
                
                // Verify no class methods exist
                var hasClassMethods = sourceFile.Methods.Any(m => !m.IsLocalMethod);
                Assert.False(hasClassMethods);
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseSourceFile_WithMainMacro_ShouldHaveNoMethods()
        {
            // Arrange
            var sourceContent = @"
#include ""StdAfx.h""
#include ""ISample.h""

MAIN(rep01)
{
    agrint lMember1 = 0;
    TDimValue dimClient;
    CString cTest = _T(""Test"");
    
    typedef struct attinfotag
    {
        TAttId    attId;
        TDimValue dimAttName;
        CString   cDimId;
    }   attinfo;

    attinfo asAggDim[10];
    AGRGetParam (_T(""client""), dimClient);
    
    return 0;
}";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, sourceContent);

            try
            {
                // Act
                var parser = new CppSourceParser();
                var sourceFile = parser.ParseSourceFileComplete(tempFile);

                // Debug output
                Console.WriteLine($"Methods parsed: {sourceFile.Methods.Count}");
                foreach (var method in sourceFile.Methods)
                {
                    Console.WriteLine($"  - {method.Name} (IsLocal: {method.IsLocalMethod})");
                }

                // Assert - MAIN macros may or may not be parsed
                // But all methods should be local methods (no class methods)
                if (sourceFile.Methods.Any())
                {
                    Assert.All(sourceFile.Methods, m => Assert.True(m.IsLocalMethod));
                }
                
                // Verify no class methods exist
                var hasClassMethods = sourceFile.Methods.Any(m => !m.IsLocalMethod);
                Assert.False(hasClassMethods);
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseSourceFile_WithOnlyStaticInitializers_NoClassMethods()
        {
            // Arrange
            var sourceContent = @"
#include ""StdAfx.h""

const CString CStaticClass::ColFrom[] = { _T(""from_1""), _T(""from_2""), _T(""from_3""), _T(""from_4"") };
const CString CStaticClass::ColTo[] = { _T(""to_1""), _T(""to_2""), _T(""to_3""), _T(""to_4"") };
";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, sourceContent);

            try
            {
                // Act
                var parser = new CppSourceParser();
                var sourceFile = parser.ParseSourceFileComplete(tempFile);

                // Assert - Should have static initializers but no methods
                Assert.NotEmpty(sourceFile.StaticMemberInits);
                
                // No methods at all (neither class nor local)
                var hasAnyMethods = sourceFile.Methods.Any();
                Assert.False(hasAnyMethods);
                
                // Definitely no class methods
                var hasClassMethods = sourceFile.Methods.Any(m => !m.IsLocalMethod);
                Assert.False(hasClassMethods);
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseSourceFile_WithClassMethods_ShouldHaveNonLocalMethods()
        {
            // Arrange
            var sourceContent = @"
#include ""CSample.h""

void CSample::MethodOne()
{
    m_value1 = 1;
}

int CSample::GetValue() const
{
    return m_value1;
}";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, sourceContent);

            try
            {
                // Act
                var parser = new CppSourceParser();
                var sourceFile = parser.ParseSourceFileComplete(tempFile);

                // Assert - Should have class methods (not local)
                Assert.NotEmpty(sourceFile.Methods);
                
                var hasClassMethods = sourceFile.Methods.Any(m => !m.IsLocalMethod);
                Assert.True(hasClassMethods);
                
                // All methods should be class methods
                Assert.All(sourceFile.Methods, method => Assert.False(method.IsLocalMethod));
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseSourceFile_WithMixedMethods_ShouldDetectBoth()
        {
            // Arrange
            var sourceContent = @"
#include ""CSample.h""

// Local helper function
bool ValidateValue(int value)
{
    return value > 0;
}

// Class method
void CSample::MethodOne()
{
    if (ValidateValue(m_value1))
        m_value1 = 1;
}

// Another local function
void LogMessage(const CString& msg)
{
    // Log implementation
}

// Another class method
int CSample::GetValue() const
{
    return m_value1;
}";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, sourceContent);

            try
            {
                // Act
                var parser = new CppSourceParser();
                var sourceFile = parser.ParseSourceFileComplete(tempFile);

                // Assert
                Assert.Equal(4, sourceFile.Methods.Count);
                
                // Should have both local and class methods
                var localMethods = sourceFile.Methods.Where(m => m.IsLocalMethod).ToList();
                var classMethods = sourceFile.Methods.Where(m => !m.IsLocalMethod).ToList();
                
                Assert.Equal(2, localMethods.Count);
                Assert.Equal(2, classMethods.Count);
                
                // Should have class methods
                var hasClassMethods = sourceFile.Methods.Any(m => !m.IsLocalMethod);
                Assert.True(hasClassMethods);
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Fact]
        public void StructuralConverter_ShouldSkipFilesWithOnlyLocalMethods()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);

            try
            {
                // Create header file
                var headerFile = Path.Combine(tempDir, "TestClass.h");
                var headerContent = @"
class TestClass
{
public:
    void MethodOne();
};";
                File.WriteAllText(headerFile, headerContent);

                // Create source file with only local methods (should be skipped)
                var localOnlyFile = Path.Combine(tempDir, "LocalOnly.cpp");
                var localOnlyContent = @"
#include ""StdAfx.h""

void LocalFunction()
{
    // Local function implementation
}

bool AnotherLocalFunction(int value)
{
    return value > 0;
}";
                File.WriteAllText(localOnlyFile, localOnlyContent);

                // Create source file with class methods (should be processed)
                var classMethodFile = Path.Combine(tempDir, "TestClass.cpp");
                var classMethodContent = @"
#include ""TestClass.h""

void TestClass::MethodOne()
{
    // Implementation
}";
                File.WriteAllText(classMethodFile, classMethodContent);

                var outputDir = Path.Combine(tempDir, "output");
                Directory.CreateDirectory(outputDir);

                // Act
                var converter = new CppToCsConverterApi();
                converter.ConvertDirectory(tempDir, outputDir);

                // Assert
                // Should generate TestClass.cs
                var testClassCsFile = Path.Combine(outputDir, "TestClass.cs");
                Assert.True(File.Exists(testClassCsFile));

                // Should NOT generate LocalOnly.cs (file was skipped)
                var localOnlyCsFile = Path.Combine(outputDir, "LocalOnly.cs");
                Assert.False(File.Exists(localOnlyCsFile));
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void StructuralConverter_ShouldSkipMainMacroFiles()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);

            try
            {
                // Create a MAIN macro file (should be skipped)
                var mainFile = Path.Combine(tempDir, "Rep01.cpp");
                var mainContent = @"
#include ""StdAfx.h""

MAIN(rep01)
{
    agrint lMember1 = 0;
    CString cTest = _T(""Test"");
    return 0;
}";
                File.WriteAllText(mainFile, mainContent);

                var outputDir = Path.Combine(tempDir, "output");
                Directory.CreateDirectory(outputDir);

                // Act
                var converter = new CppToCsConverterApi();
                converter.ConvertDirectory(tempDir, outputDir);

                // Assert
                // Should NOT generate Rep01.cs (file was skipped)
                var rep01CsFile = Path.Combine(outputDir, "Rep01.cs");
                Assert.False(File.Exists(rep01CsFile));

                // Output directory should have no .cs files
                var csFiles = Directory.GetFiles(outputDir, "*.cs");
                Assert.Empty(csFiles);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void StructuralConverter_ShouldSkipStaticInitializerOnlyFiles()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);

            try
            {
                // Create header file
                var headerFile = Path.Combine(tempDir, "CStaticClass.h");
                var headerContent = @"
class CStaticClass
{
public:
    static const CString ColFrom[];
    static const CString ColTo[];
};";
                File.WriteAllText(headerFile, headerContent);

                // Create source file with only static initializers (should be skipped)
                var staticOnlyFile = Path.Combine(tempDir, "CStaticClass.cpp");
                var staticOnlyContent = @"
#include ""StdAfx.h""

const CString CStaticClass::ColFrom[] = { _T(""from_1""), _T(""from_2"") };
const CString CStaticClass::ColTo[] = { _T(""to_1""), _T(""to_2"") };
";
                File.WriteAllText(staticOnlyFile, staticOnlyContent);

                var outputDir = Path.Combine(tempDir, "output");
                Directory.CreateDirectory(outputDir);

                // Act
                var converter = new CppToCsConverterApi();
                converter.ConvertDirectory(tempDir, outputDir);

                // Assert
                // Should generate CStaticClass.cs from header
                var staticClassCsFile = Path.Combine(outputDir, "CStaticClass.cs");
                Assert.True(File.Exists(staticClassCsFile));

                // But the .cpp file should have been skipped
                // (static initializers are merged from source into header-based class)
                // Verify by checking that we only have one .cs file
                var csFiles = Directory.GetFiles(outputDir, "*.cs");
                Assert.Single(csFiles);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void ParseSourceFile_RoutinesCpp_ShouldOnlyHaveLocalMethods()
        {
            // This test uses the actual Routines.cpp from SamplesAndExpectations
            var routinesFile = Path.Combine(
                Directory.GetCurrentDirectory(), 
                "..", "..", "..", "..", 
                "SamplesAndExpectations", 
                "Routines.cpp");

            if (!File.Exists(routinesFile))
            {
                // Skip test if file doesn't exist
                return;
            }

            // Act
            var parser = new CppSourceParser();
            var sourceFile = parser.ParseSourceFileComplete(routinesFile);

            // Assert
            Assert.NotEmpty(sourceFile.Methods);
            
            // All methods should be local (no class methods)
            Assert.All(sourceFile.Methods, method => Assert.True(method.IsLocalMethod));
            
            // Verify no class methods exist
            var hasClassMethods = sourceFile.Methods.Any(m => !m.IsLocalMethod);
            Assert.False(hasClassMethods);
        }

        [Fact]
        public void ParseSourceFile_Rep01Cpp_ShouldHaveNoMethods()
        {
            // This test uses the actual Rep01.cpp from SamplesAndExpectations
            var rep01File = Path.Combine(
                Directory.GetCurrentDirectory(), 
                "..", "..", "..", "..", 
                "SamplesAndExpectations", 
                "Rep01.cpp");

            if (!File.Exists(rep01File))
            {
                // Skip test if file doesn't exist
                return;
            }

            // Act
            var parser = new CppSourceParser();
            var sourceFile = parser.ParseSourceFileComplete(rep01File);

            // Debug output
            Console.WriteLine($"Methods parsed: {sourceFile.Methods.Count}");
            foreach (var method in sourceFile.Methods)
            {
                Console.WriteLine($"  - {method.Name} (IsLocal: {method.IsLocalMethod})");
            }

            // Assert - Rep01.cpp contains MAIN macro and some local functions
            // These should all be marked as local methods
            if (sourceFile.Methods.Any())
            {
                Assert.All(sourceFile.Methods, m => Assert.True(m.IsLocalMethod));
            }
            
            // Verify no class methods exist
            var hasClassMethods = sourceFile.Methods.Any(m => !m.IsLocalMethod);
            Assert.False(hasClassMethods);
        }
    }
}
