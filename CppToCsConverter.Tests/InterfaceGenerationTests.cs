using System.IO;
using System.Linq;
using Xunit;
using CppToCsConverter.Core.Generators;
using CppToCsConverter.Core.Models;
using CppToCsConverter.Core.Parsers;

namespace CppToCsConverter.Tests
{
    /// <summary>
    /// Tests for C# interface generation based on readme.md examples.
    /// Covers public/internal interfaces, static methods as extensions, and pure virtual detection.
    /// </summary>
    public class InterfaceGenerationTests
    {
        private readonly CsInterfaceGenerator _generator;
        private readonly CppHeaderParser _parser;

        public InterfaceGenerationTests()
        {
            _generator = new CsInterfaceGenerator();
            _parser = new CppHeaderParser();
        }

        [Fact]
        public void GenerateInterface_PublicInterface_ShouldCreatePublicInterface()
        {
            // Arrange - Based on readme.md public interface example
            var headerContent = @"
class __declspec(dllexport) ISample
{
public:
    virtual ~ISample(){};
    static ISample* GetInstance();
    virtual void MethodOne(const CString& cParam1, const bool &bParam2, CString *pcParam3) = 0;
    virtual bool MethodTwo() = 0;
};";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, headerContent);

            try
            {
                // Act
                var classes = _parser.ParseHeaderFile(tempFile);
                var interfaceClass = classes.FirstOrDefault(c => c.Name == "ISample");
                Assert.NotNull(interfaceClass);

                var result = _generator.GenerateInterface(interfaceClass);

                // Assert
                Assert.Contains("public interface ISample", result);
                Assert.Contains("void MethodOne(", result);
                Assert.Contains("bool MethodTwo()", result);
                Assert.Contains("namespace GeneratedInterfaces", result);
                
                // Should not contain destructor or static methods in interface
                Assert.DoesNotContain("~ISample", result);
                Assert.DoesNotContain("GetInstance", result); // Static methods not in interface or extension class
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void GenerateInterface_InternalInterface_ShouldCreateInternalInterface()
        {
            // Arrange - Based on readme.md internal interface example (no __declspec(dllexport))
            var headerContent = @"
class ISample
{
public:
    virtual ~ISample(){};
    static ISample* GetInstance();
    virtual void MethodOne(const CString& cParam1, const bool &bParam2, CString *pcParam3) = 0;
    virtual bool MethodTwo() = 0;
};";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, headerContent);

            try
            {
                // Act
                var classes = _parser.ParseHeaderFile(tempFile);
                var interfaceClass = classes.FirstOrDefault(c => c.Name == "ISample");
                Assert.NotNull(interfaceClass);

                var result = _generator.GenerateInterface(interfaceClass);

                // Assert
                Assert.Contains("internal interface ISample", result);
                Assert.Contains("void MethodOne(", result);
                Assert.Contains("bool MethodTwo()", result);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void GenerateInterface_WithStaticMethods_ShouldNotCreateExtensionsClass()
        {
            // Arrange - Based on updated requirement: no extension classes, use Create attribute instead
            var headerContent = @"
class __declspec(dllexport) ISample
{
public:
    virtual ~ISample(){};
    static ISample* GetInstance();
    virtual void MethodOne(const CString& cParam1, const bool &bParam2, CString *pcParam3) = 0;
    virtual bool MethodTwo() = 0;
};";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, headerContent);

            try
            {
                // Act
                var classes = _parser.ParseHeaderFile(tempFile);
                var interfaceClass = classes.FirstOrDefault(c => c.Name == "ISample");
                Assert.NotNull(interfaceClass);

                var result = _generator.GenerateInterface(interfaceClass);

                // Assert - Should NOT create extensions class anymore
                Assert.DoesNotContain("ISampleExtensions", result);
                Assert.DoesNotContain("public static ISample GetInstance(", result);
                
                // Static method should not be in the interface definition either
                Assert.DoesNotContain("GetInstance", result);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void GenerateInterface_PureVirtualMethods_ShouldBeIncludedInInterface()
        {
            // Arrange - Test pure virtual detection (= 0)
            var headerContent = @"
class ITestInterface
{
public:
    virtual void PureMethod() = 0;
    virtual bool PureBoolMethod(int param) = 0;
    virtual void RegularMethod() {} // Not pure virtual
};";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, headerContent);

            try
            {
                // Act
                var classes = _parser.ParseHeaderFile(tempFile);
                var interfaceClass = classes.FirstOrDefault(c => c.Name == "ITestInterface");
                Assert.NotNull(interfaceClass);

                var result = _generator.GenerateInterface(interfaceClass);

                // Assert
                Assert.Contains("void PureMethod();", result);
                Assert.Contains("bool PureBoolMethod(int param);", result);
                
                // Regular method with body should also be included if it's public virtual
                Assert.Contains("void RegularMethod();", result);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void GenerateInterface_MethodParameters_ShouldHandleComplexTypes()
        {
            // Arrange - Based on readme parameter examples
            var headerContent = @"
class IComplexInterface
{
public:
    virtual void ComplexMethod(const TDimValue& dim1, 
                              const agrint& int1, 
                              const agrint& int2 = 0, 
                              bool bool1 = false) = 0;
};";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, headerContent);

            try
            {
                // Act
                var classes = _parser.ParseHeaderFile(tempFile);
                var interfaceClass = classes.FirstOrDefault(c => c.Name == "IComplexInterface");
                Assert.NotNull(interfaceClass);

                var result = _generator.GenerateInterface(interfaceClass);

                // Assert - Should preserve parameter structure
                Assert.Contains("void ComplexMethod(", result);
                Assert.Contains("TDimValue", result);
                Assert.Contains("agrint", result);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }
    }
}