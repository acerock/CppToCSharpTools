using System.IO;
using Xunit;
using Xunit.Abstractions;
using CppToCsConverter.Core.Parsers;

namespace CppToCsConverter.Tests
{
    public class PointerReturnTypeTests
    {
        private readonly ITestOutputHelper _output;

        public PointerReturnTypeTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void ParseInterface_WithPointerReturnType_ShouldCaptureReturnType()
        {
            // Arrange - Real-world case from IAgrLibHSEx2.h
            string headerContent = @"
#pragma once

class __declspec(dllexport) IAgrLibHSEx2
{
public:
    virtual CAgrMT *GetCustomResTable(const CString &cName) = 0;
    virtual CAgrMT *GetCustomCommonTable(const CString &cName) = 0;
    virtual CString GetResourceTable() = 0;
};
";

            string tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, headerContent);

                // Act
                var parser = new CppHeaderParser();
                var classes = parser.ParseHeaderFile(tempFile);

                // Assert
                Assert.Single(classes);
                var interfaceClass = classes[0];
                Assert.Equal("IAgrLibHSEx2", interfaceClass.Name);
                Assert.Equal(3, interfaceClass.Methods.Count);

                // Debug output
                foreach (var method in interfaceClass.Methods)
                {
                    _output.WriteLine($"Method: {method.Name}, ReturnType: '{method.ReturnType}'");
                }

                // Check pointer return types
                var method1 = interfaceClass.Methods.Find(m => m.Name == "GetCustomResTable");
                Assert.NotNull(method1);
                Assert.Equal("CAgrMT *", method1.ReturnType); // Should be "CAgrMT *", not null or ""

                var method2 = interfaceClass.Methods.Find(m => m.Name == "GetCustomCommonTable");
                Assert.NotNull(method2);
                Assert.Equal("CAgrMT *", method2.ReturnType);

                var method3 = interfaceClass.Methods.Find(m => m.Name == "GetResourceTable");
                Assert.NotNull(method3);
                Assert.Equal("CString", method3.ReturnType);
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseInterface_PointerReturnTypeVariations_ShouldCaptureAll()
        {
            // Arrange - Test different pointer/reference spacing variations
            string headerContent = @"
#pragma once

class __declspec(dllexport) ITestPointers
{
public:
    virtual CAgrMT* Method1() = 0;          // No space before *
    virtual CAgrMT *Method2() = 0;          // Space before *
    virtual CAgrMT * Method3() = 0;         // Spaces around *
    virtual CAgrMT& Method4() = 0;          // Reference no space
    virtual CAgrMT &Method5() = 0;          // Reference with space
    virtual CAgrMT * * Method6() = 0;       // Pointer to pointer
    virtual const CString* Method7() = 0;   // Const pointer
    virtual const CString *Method8() = 0;   // Const pointer with space
};
";

            string tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, headerContent);

                // Act
                var parser = new CppHeaderParser();
                var classes = parser.ParseHeaderFile(tempFile);

                // Assert
                Assert.Single(classes);
                var interfaceClass = classes[0];
                Assert.Equal(8, interfaceClass.Methods.Count);

                // Debug output
                _output.WriteLine("Parsed return types:");
                foreach (var method in interfaceClass.Methods)
                {
                    _output.WriteLine($"  {method.Name}: '{method.ReturnType}'");
                }

                // Verify each variation
                Assert.Equal("CAgrMT*", interfaceClass.Methods[0].ReturnType);
                Assert.Equal("CAgrMT *", interfaceClass.Methods[1].ReturnType);
                Assert.Equal("CAgrMT *", interfaceClass.Methods[2].ReturnType);
                Assert.Equal("CAgrMT&", interfaceClass.Methods[3].ReturnType);
                Assert.Equal("CAgrMT &", interfaceClass.Methods[4].ReturnType);
                Assert.Equal("CAgrMT * *", interfaceClass.Methods[5].ReturnType);
                Assert.Contains("CString*", interfaceClass.Methods[6].ReturnType); // Should include const
                Assert.Contains("CString *", interfaceClass.Methods[7].ReturnType); // Should include const
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }
    }
}
