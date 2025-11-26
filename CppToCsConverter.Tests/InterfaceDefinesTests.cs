using System.IO;
using System.Linq;
using Xunit;
using CppToCsConverter.Core.Core;

namespace CppToCsConverter.Tests
{
    /// <summary>
    /// Tests for interface header defines extraction to separate static class files.
    /// Based on readme.md: defines from interface headers should become public const members 
    /// in a public static class named [InterfaceName]Defines.cs
    /// </summary>
    public class InterfaceDefinesTests
    {
        [Fact]
        public void InterfaceHeader_WithDefines_ShouldGenerateSeparateDefinesClass()
        {
            // Arrange - Interface with defines
            var headerContent = @"
#pragma once

// Here are some defines
// Comment for warning
#define WARNING 1
// Comment for stop
#define STOP 2
#define STOP_ALL 4

class __declspec(dllexport) ISample
{
public:
    virtual ~ISample(){};
    static ISample* GetInstance();
    virtual void MethodOne() = 0;
    virtual bool MethodTwo() = 0;
};

// Some more defines
#define MY_DEFINE4 4
#define MY_DEFINE5 5
";

            var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);

            try
            {
                var headerFile = Path.Combine(tempDir, "ISample.h");
                File.WriteAllText(headerFile, headerContent);

                var outputDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                Directory.CreateDirectory(outputDir);

                // Act
                var converter = new CppToCsStructuralConverter();
                converter.ConvertDirectory(tempDir, outputDir);

                // Assert - Should generate two files
                var interfaceFile = Path.Combine(outputDir, "ISample.cs");
                var definesFile = Path.Combine(outputDir, "SampleDefines.cs");

                Assert.True(File.Exists(interfaceFile), "ISample.cs should be generated");
                Assert.True(File.Exists(definesFile), "SampleDefines.cs should be generated");

                var definesContent = File.ReadAllText(definesFile);

                // Should be public static class
                Assert.Contains("public static class SampleDefines", definesContent);

                // Should have public const members
                Assert.Contains("public const int WARNING = 1", definesContent);
                Assert.Contains("public const int STOP = 2", definesContent);
                Assert.Contains("public const int STOP_ALL = 4", definesContent);
                Assert.Contains("public const int MY_DEFINE4 = 4", definesContent);
                Assert.Contains("public const int MY_DEFINE5 = 5", definesContent);

                // Should preserve comments
                Assert.Contains("// Here are some defines", definesContent);
                Assert.Contains("// Comment for warning", definesContent);
                Assert.Contains("// Comment for stop", definesContent);
                Assert.Contains("// Some more defines", definesContent);

                // Interface file should NOT contain defines
                var interfaceContent = File.ReadAllText(interfaceFile);
                Assert.DoesNotContain("WARNING", interfaceContent);
                Assert.DoesNotContain("STOP", interfaceContent);

                Directory.Delete(tempDir, true);
                Directory.Delete(outputDir, true);
            }
            catch
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
                throw;
            }
        }

        [Fact]
        public void PublicInterface_WithDefines_ShouldAddUsingStaticToOtherClasses()
        {
            // Arrange - Interface with defines AND a class in same directory
            var interfaceHeader = @"
#pragma once

#define WARNING 1
#define STOP 2

class __declspec(dllexport) ISample
{
public:
    virtual ~ISample(){};
    virtual void MethodOne() = 0;
};
";

            var classHeader = @"
#pragma once
#include ""ISample.h""

class CSample : public ISample
{
public:
    CSample();
    void MethodOne();
};
";

            var classSource = @"
#include ""CSample.h""

CSample::CSample()
{
}

void CSample::MethodOne()
{
    // Implementation
}
";

            var tempDir = Path.Combine(Path.GetTempPath(), "AgrLibHS");
            Directory.CreateDirectory(tempDir);

            try
            {
                File.WriteAllText(Path.Combine(tempDir, "ISample.h"), interfaceHeader);
                File.WriteAllText(Path.Combine(tempDir, "CSample.h"), classHeader);
                File.WriteAllText(Path.Combine(tempDir, "CSample.cpp"), classSource);

                var outputDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                Directory.CreateDirectory(outputDir);

                // Act
                var converter = new CppToCsStructuralConverter();
                converter.ConvertDirectory(tempDir, outputDir);

                // Assert
                var definesFile = Path.Combine(outputDir, "SampleDefines.cs");
                var classFile = Path.Combine(outputDir, "CSample.cs");

                Assert.True(File.Exists(definesFile), "SampleDefines.cs should be generated");
                Assert.True(File.Exists(classFile), "CSample.cs should be generated");

                var classContent = File.ReadAllText(classFile);

                // Should have using static for defines class
                Assert.Contains("using static U4.BatchNet.HS.Compatibility.SampleDefines;", classContent);

                Directory.Delete(tempDir, true);
                Directory.Delete(outputDir, true);
            }
            catch
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
                throw;
            }
        }

        [Fact]
        public void InternalInterface_WithDefines_ShouldNotGenerateSeparateDefinesClass()
        {
            // Arrange - Internal interface (no __declspec(dllexport)) with defines
            var headerContent = @"
#pragma once

#define INTERNAL_DEFINE 100

class ISample
{
public:
    virtual ~ISample(){};
    virtual void MethodOne() = 0;
};
";

            var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);

            try
            {
                var headerFile = Path.Combine(tempDir, "ISample.h");
                File.WriteAllText(headerFile, headerContent);

                var outputDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                Directory.CreateDirectory(outputDir);

                // Act
                var converter = new CppToCsStructuralConverter();
                converter.ConvertDirectory(tempDir, outputDir);

                // Assert - Should NOT generate separate defines file
                var definesFile = Path.Combine(outputDir, "SampleDefines.cs");
                Assert.False(File.Exists(definesFile), "SampleDefines.cs should NOT be generated for internal interface");

                // Defines should be in the interface file as internal const
                var interfaceFile = Path.Combine(outputDir, "ISample.cs");
                var interfaceContent = File.ReadAllText(interfaceFile);
                Assert.Contains("internal const int INTERNAL_DEFINE = 100", interfaceContent);

                Directory.Delete(tempDir, true);
                Directory.Delete(outputDir, true);
            }
            catch
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
                throw;
            }
        }

        [Fact]
        public void PublicInterface_NoDefines_ShouldNotGenerateDefinesClass()
        {
            // Arrange - Public interface without defines
            var headerContent = @"
#pragma once

class __declspec(dllexport) ISample
{
public:
    virtual ~ISample(){};
    virtual void MethodOne() = 0;
};
";

            var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);

            try
            {
                var headerFile = Path.Combine(tempDir, "ISample.h");
                File.WriteAllText(headerFile, headerContent);

                var outputDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                Directory.CreateDirectory(outputDir);

                // Act
                var converter = new CppToCsStructuralConverter();
                converter.ConvertDirectory(tempDir, outputDir);

                // Assert - Should NOT generate defines file
                var definesFile = Path.Combine(outputDir, "SampleDefines.cs");
                Assert.False(File.Exists(definesFile), "SampleDefines.cs should NOT be generated when no defines exist");

                Directory.Delete(tempDir, true);
                Directory.Delete(outputDir, true);
            }
            catch
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
                throw;
            }
        }

        [Fact]
        public void InterfaceDefinesClass_ShouldHaveCorrectNamespaceAndUsings()
        {
            // Arrange
            var headerContent = @"
#pragma once

#define MY_CONST 42

class __declspec(dllexport) ISample
{
public:
    virtual void Method() = 0;
};
";

            var tempDir = Path.Combine(Path.GetTempPath(), "AgrLibHS");
            Directory.CreateDirectory(tempDir);

            try
            {
                var headerFile = Path.Combine(tempDir, "ISample.h");
                File.WriteAllText(headerFile, headerContent);

                var outputDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                Directory.CreateDirectory(outputDir);

                // Act
                var converter = new CppToCsStructuralConverter();
                converter.ConvertDirectory(tempDir, outputDir);

                // Assert
                var definesFile = Path.Combine(outputDir, "SampleDefines.cs");
                Assert.True(File.Exists(definesFile));

                var content = File.ReadAllText(definesFile);

                // Should have correct namespace (HS from AgrLibHS)
                Assert.Contains("namespace U4.BatchNet.HS.Compatibility", content);

                // Should have using statements
                Assert.Contains("using Agresso.Types;", content);
                Assert.Contains("using BatchNet;", content);

                Directory.Delete(tempDir, true);
                Directory.Delete(outputDir, true);
            }
            catch
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
                throw;
            }
        }
    }
}
