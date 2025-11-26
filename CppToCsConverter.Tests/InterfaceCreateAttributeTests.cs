using System.IO;
using System.Linq;
using Xunit;
using CppToCsConverter.Core.Core;

namespace CppToCsConverter.Tests
{
    /// <summary>
    /// Tests for Create attribute generation on public interfaces.
    /// Based on new requirement: public interfaces should have [Create(typeof(ImplementingClass))] attribute
    /// instead of extension classes.
    /// </summary>
    public class InterfaceCreateAttributeTests
    {
        private readonly CppToCsStructuralConverter _converter;

        public InterfaceCreateAttributeTests()
        {
            _converter = new CppToCsStructuralConverter();
        }

        [Fact]
        public void PublicInterface_WithStaticFactory_ShouldHaveCreateAttribute()
        {
            // Arrange
            string headerContent = @"
class __declspec(dllexport) ISample
{
public:
    virtual ~ISample(){};
    static ISample* GetInstance();
    virtual void MethodOne() = 0;
    virtual bool MethodTwo() = 0;
};";

            string sourceContent = @"
ISample* ISample::GetInstance()
{
    CSample* pSample = new CSample();
    return pSample;
}";

            var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);

            var headerFile = Path.Combine(tempDir, "ISample.h");
            var sourceFile = Path.Combine(tempDir, "ISample.cpp");

            File.WriteAllText(headerFile, headerContent);
            File.WriteAllText(sourceFile, sourceContent);

            try
            {
                // Act
                _converter.ConvertDirectory(tempDir, tempDir);

                // Assert
                var csFile = Path.Combine(tempDir, "ISample.cs");
                Assert.True(File.Exists(csFile), "ISample.cs should be generated");

                var content = File.ReadAllText(csFile);
                
                // Should have Create attribute with resolved class
                Assert.Contains("[Create(typeof(CSample))]", content);
                
                // Should be public interface
                Assert.Contains("public interface ISample", content);
                
                // Should have interface methods
                Assert.Contains("void MethodOne()", content);
                Assert.Contains("bool MethodTwo()", content);
                
                // Should NOT have extension class
                Assert.DoesNotContain("ISampleExtensions", content);
                Assert.DoesNotContain("public static class", content);
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
        public void InternalInterface_ShouldNotHaveCreateAttribute()
        {
            // Arrange
            string headerContent = @"
class ISample
{
public:
    virtual ~ISample(){};
    static ISample* GetInstance();
    virtual void MethodOne() = 0;
};";

            string sourceContent = @"
ISample* ISample::GetInstance()
{
    CSample* pSample = new CSample();
    return pSample;
}";

            var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);

            var headerFile = Path.Combine(tempDir, "ISample.h");
            var sourceFile = Path.Combine(tempDir, "ISample.cpp");

            File.WriteAllText(headerFile, headerContent);
            File.WriteAllText(sourceFile, sourceContent);

            try
            {
                // Act
                _converter.ConvertDirectory(tempDir, tempDir);

                // Assert
                var csFile = Path.Combine(tempDir, "ISample.cs");
                Assert.True(File.Exists(csFile), "ISample.cs should be generated");

                var content = File.ReadAllText(csFile);
                
                // Should NOT have Create attribute
                Assert.DoesNotContain("[Create(typeof(", content);
                
                // Should be internal interface
                Assert.Contains("internal interface ISample", content);
                
                // Should NOT have extension class
                Assert.DoesNotContain("ISampleExtensions", content);
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
        public void PublicInterface_MultipleFactoryReturns_ShouldResolveCorrectClass()
        {
            // Arrange
            string headerContent = @"
class __declspec(dllexport) ISample
{
public:
    virtual ~ISample(){};
    static ISample* GetInstance();
    virtual void DoSomething() = 0;
};";

            string sourceContent = @"
ISample* ISample::GetInstance()
{
    // Multiple lines before return
    bool condition = true;
    if (condition)
    {
        CAdvancedSample* pSample = new CAdvancedSample();
        return pSample;
    }
    return nullptr;
}";

            var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);

            var headerFile = Path.Combine(tempDir, "ISample.h");
            var sourceFile = Path.Combine(tempDir, "ISample.cpp");

            File.WriteAllText(headerFile, headerContent);
            File.WriteAllText(sourceFile, sourceContent);

            try
            {
                // Act
                _converter.ConvertDirectory(tempDir, tempDir);

                // Assert
                var csFile = Path.Combine(tempDir, "ISample.cs");
                var content = File.ReadAllText(csFile);
                
                // Should resolve CAdvancedSample from factory method
                Assert.Contains("[Create(typeof(CAdvancedSample))]", content);
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
        public void PublicInterface_NoFactory_ShouldNotHaveCreateAttribute()
        {
            // Arrange
            string headerContent = @"
class __declspec(dllexport) ISample
{
public:
    virtual ~ISample(){};
    virtual void MethodOne() = 0;
};";

            var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);

            var headerFile = Path.Combine(tempDir, "ISample.h");
            File.WriteAllText(headerFile, headerContent);

            try
            {
                // Act
                _converter.ConvertDirectory(tempDir, tempDir);

                // Assert
                var csFile = Path.Combine(tempDir, "ISample.cs");
                var content = File.ReadAllText(csFile);
                
                // Should be public but without Create attribute (no factory found)
                Assert.Contains("public interface ISample", content);
                Assert.DoesNotContain("[Create(typeof(", content);
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
