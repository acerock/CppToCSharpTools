using System.IO;
using System.Linq;
using Xunit;
using CppToCsConverter.Core.Generators;
using CppToCsConverter.Core.Models;
using CppToCsConverter.Core.Parsers;

namespace CppToCsConverter.Tests
{
    /// <summary>
    /// Tests for C# class generation based on readme.md examples.
    /// Covers inheritance, access modifiers, static members, inline methods, and partial classes.
    /// </summary>
    public class ClassGenerationTests
    {
        private readonly CsClassGenerator _generator;
        private readonly CppHeaderParser _headerParser;
        private readonly CppSourceParser _sourceParser;

        public ClassGenerationTests()
        {
            _generator = new CsClassGenerator();
            _headerParser = new CppHeaderParser();
            _sourceParser = new CppSourceParser();
        }

        [Fact]
        public void GenerateClass_WithInheritance_ShouldIncludeBaseClass()
        {
            // Arrange - Based on readme.md CSample : ISample example
            var headerContent = @"
class CSample : public ISample
{
private:
    agrint m_value1;
    CString cValue1;

public:
    CSample();
    void MethodOne();
    bool MethodTwo() { return cValue1 != """"; };
};";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, headerContent);

            try
            {
                // Act
                var classes = _headerParser.ParseHeaderFile(tempFile);
                var cppClass = classes.FirstOrDefault(c => c.Name == "CSample");
                Assert.NotNull(cppClass);

                var result = _generator.GenerateClass(cppClass, new System.Collections.Generic.List<CppMethod>(), "CSample");

                // Assert
                Assert.Contains("public class CSample : ISample", result);
                Assert.Contains("namespace GeneratedClasses", result);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void GenerateClass_WithAccessModifiers_ShouldPreserveAccessibility()
        {
            // Arrange - Based on readme.md access modifier examples
            var headerContent = @"
class CSample
{
private:
    agrint m_value1;
    CString cValue1;

public:
    CSample();
    void PublicMethod();

private:
    bool PrivateMethod();
};";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, headerContent);

            try
            {
                // Act
                var classes = _headerParser.ParseHeaderFile(tempFile);
                var cppClass = classes.FirstOrDefault(c => c.Name == "CSample");
                Assert.NotNull(cppClass);

                var result = _generator.GenerateClass(cppClass, new System.Collections.Generic.List<CppMethod>(), "CSample");

                // Assert
                Assert.Contains("private agrint m_value1;", result);
                Assert.Contains("private CString cValue1;", result);
                Assert.Contains("public CSample();", result);
                Assert.Contains("public void PublicMethod();", result);
                Assert.Contains("private bool PrivateMethod();", result);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void GenerateClass_WithInlineMethod_ShouldIncludeBody()
        {
            // Arrange - Based on readme.md inline method example
            var headerContent = @"
class CSample
{
public:
    bool MethodTwo() { return cValue1 != """"; };
private:
    CString cValue1;
};";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, headerContent);

            try
            {
                // Act
                var classes = _headerParser.ParseHeaderFile(tempFile);
                var cppClass = classes.FirstOrDefault(c => c.Name == "CSample");
                Assert.NotNull(cppClass);

                var result = _generator.GenerateClass(cppClass, new System.Collections.Generic.List<CppMethod>(), "CSample");

                // Assert
                Assert.Contains("bool MethodTwo()", result);
                Assert.Contains("return cValue1 != \"\";", result);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void GeneratePartialClass_ShouldAddPartialKeyword()
        {
            // Arrange - Based on readme.md partial class example
            var headerContent = @"
class CSample : public ISample
{
public:
    void MethodOne();
    void MethodTwo();
};";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, headerContent);

            try
            {
                // Act
                var classes = _headerParser.ParseHeaderFile(tempFile);
                var cppClass = classes.FirstOrDefault(c => c.Name == "CSample");
                Assert.NotNull(cppClass);

                var result = _generator.GeneratePartialClass(cppClass, new System.Collections.Generic.List<CppMethod>(), "CSample");

                // Assert
                Assert.Contains("public partial class CSample : ISample", result);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void GenerateClass_WithStaticMembers_ShouldPreserveStaticModifier()
        {
            // Arrange - Based on readme.md static member example
            var headerContent = @"
class CSample
{
private:
    static agrint s_value;
    agrint m_instanceValue;

public:
    static void StaticMethod();
    void InstanceMethod();
};";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, headerContent);

            try
            {
                // Act
                var classes = _headerParser.ParseHeaderFile(tempFile);
                var cppClass = classes.FirstOrDefault(c => c.Name == "CSample");
                Assert.NotNull(cppClass);

                var result = _generator.GenerateClass(cppClass, new System.Collections.Generic.List<CppMethod>(), "CSample");

                // Assert
                Assert.Contains("private static agrint s_value;", result);
                Assert.Contains("private agrint m_instanceValue;", result);
                Assert.Contains("public static void StaticMethod();", result);
                Assert.Contains("public void InstanceMethod();", result);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void GenerateClass_WithDefaultParameters_ShouldPreserveDefaults()
        {
            // Arrange - Based on readme.md default parameter example
            var headerContent = @"
class CSample
{
private:
    bool MethodP1(const TDimValue& dim1, const agrint& int1, const agrint& int2=0, bool bool1=false);
};";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, headerContent);

            try
            {
                // Act
                var classes = _headerParser.ParseHeaderFile(tempFile);
                var cppClass = classes.FirstOrDefault(c => c.Name == "CSample");
                Assert.NotNull(cppClass);

                var result = _generator.GenerateClass(cppClass, new System.Collections.Generic.List<CppMethod>(), "CSample");

                // Assert
                Assert.Contains("MethodP1(", result);
                // Should preserve the method signature structure
                var methodP1 = cppClass.Methods.FirstOrDefault(m => m.Name == "MethodP1");
                Assert.NotNull(methodP1);
                Assert.Equal(4, methodP1.Parameters.Count);
                Assert.Equal("0", methodP1.Parameters[2].DefaultValue);
                Assert.Equal("false", methodP1.Parameters[3].DefaultValue);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void GenerateClass_WithMethodImplementations_ShouldIncludeBodies()
        {
            // Arrange - Based on readme.md method implementation merging example
            var headerContent = @"
class CSample
{
public:
    void MethodOne();
};";

            var sourceContent = @"
void CSample::MethodOne()
{
    m_value1 = 1;
}";

            var headerFile = Path.GetTempFileName();
            var sourceFile = Path.GetTempFileName();
            File.WriteAllText(headerFile, headerContent);
            File.WriteAllText(sourceFile, sourceContent);

            try
            {
                // Act
                var classes = _headerParser.ParseHeaderFile(headerFile);
                var (implementations, staticInits) = _sourceParser.ParseSourceFile(sourceFile);
                var cppClass = classes.FirstOrDefault(c => c.Name == "CSample");
                Assert.NotNull(cppClass);

                var result = _generator.GenerateClass(cppClass, implementations, "CSample");

                // Assert
                Assert.Contains("public void MethodOne()", result);
                Assert.Contains("m_value1 = 1;", result);
            }
            finally
            {
                File.Delete(headerFile);
                File.Delete(sourceFile);
            }
        }
    }
}