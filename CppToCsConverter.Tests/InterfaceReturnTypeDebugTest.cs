using Xunit;
using CppToCsConverter.Core.Parsers;
using System.IO;
using System.Linq;

namespace CppToCsConverter.Tests
{
    public class InterfaceReturnTypeDebugTest
    {
        [Fact]
        public void ParseInterface_WithVoidAndBoolMethods_ShouldCaptureReturnTypes()
        {
            // Arrange
            var headerContent = @"
#pragma once

class __declspec(dllexport) ISample
{
public:
    virtual ~ISample(){};
    static ISample* GetInstance();

    virtual void MethodOne(const CString& cParam1) = 0;
    virtual bool MethodTwo() = 0;
    virtual agrint MethodThree() = 0;
};
";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, headerContent);

            try
            {
                // Act
                var parser = new CppHeaderParser();
                var classes = parser.ParseHeaderFile(tempFile);

                // Assert
                Assert.Single(classes);
                var interfaceClass = classes[0];
                Assert.Equal("ISample", interfaceClass.Name);
                Assert.True(interfaceClass.IsInterface);

                // Get the actual methods (not constructor/destructor/static)
                var methods = interfaceClass.Methods
                    .Where(m => !m.IsConstructor && !m.IsDestructor && !m.IsStatic)
                    .ToList();

                Assert.Equal(3, methods.Count);

                // Debug output
                foreach (var method in methods)
                {
                    System.Console.WriteLine($"Method: {method.Name}, ReturnType: '{method.ReturnType ?? "NULL"}'");
                }

                // Check each method's return type
                var methodOne = methods.FirstOrDefault(m => m.Name == "MethodOne");
                Assert.NotNull(methodOne);
                Assert.Equal("void", methodOne.ReturnType);

                var methodTwo = methods.FirstOrDefault(m => m.Name == "MethodTwo");
                Assert.NotNull(methodTwo);
                Assert.Equal("bool", methodTwo.ReturnType);

                var methodThree = methods.FirstOrDefault(m => m.Name == "MethodThree");
                Assert.NotNull(methodThree);
                Assert.Equal("agrint", methodThree.ReturnType);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }
    }
}
