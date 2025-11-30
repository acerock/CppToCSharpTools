using System.IO;
using Xunit;
using CppToCsConverter.Core;
using System.Linq;

namespace CppToCsConverter.Tests
{
    public class MultipleInterfaceFilesTests
    {
        [Fact]
        public void MultipleInterfaceFiles_ProcessedSequentially_ShouldPreserveReturnTypesIndependently()
        {
            // Arrange - Create multiple interface header files similar to ISample, ISampleEx, ISampleEx2
            string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);

            try
            {
                // ISample.h - First interface with void and bool methods
                string iSampleHeader = @"
#pragma once

class __declspec(dllexport) ISample
{
public:
    virtual ~ISample(){};
    static ISample* GetInstance();

    virtual void MethodOne(const CString& cParam1) = 0;
    virtual bool MethodTwo() = 0;
};
";
                File.WriteAllText(Path.Combine(tempDir, "ISample.h"), iSampleHeader);

                // ISampleEx.h - Extended interface with agrint and CString methods
                string iSampleExHeader = @"
#pragma once

class __declspec(dllexport) ISampleEx
{
public:
    virtual ~ISampleEx(){};
    static ISampleEx* GetInstance();

    virtual agrint MethodExOne(const agrint& value) = 0;
    virtual CString MethodExTwo() = 0;
};
";
                File.WriteAllText(Path.Combine(tempDir, "ISampleEx.h"), iSampleExHeader);

                // ISampleEx2.h - Another interface with different return types
                string iSampleEx2Header = @"
#pragma once

class __declspec(dllexport) ISampleEx2
{
public:
    virtual ~ISampleEx2(){};
    static ISampleEx2* GetInstance();

    virtual bool MethodEx2One() = 0;
    virtual agrint MethodEx2Two(const bool& flag) = 0;
    virtual void MethodEx2Three() = 0;
};
";
                File.WriteAllText(Path.Combine(tempDir, "ISampleEx2.h"), iSampleEx2Header);

                string outputDir = Path.Combine(tempDir, "Generated_CS");

                // Act - Process all files in one conversion operation
                var api = new CppToCsConverterApi();
                api.ConvertDirectory(tempDir, outputDir);

                // Assert - Check each interface file independently
                
                // ISample.cs should have void and bool
                string iSampleCs = File.ReadAllText(Path.Combine(outputDir, "ISample.cs"));
                Assert.Contains("void MethodOne(const CString& cParam1);", iSampleCs);
                Assert.Contains("bool MethodTwo();", iSampleCs);
                Assert.DoesNotContain("void MethodTwo();", iSampleCs); // Should NOT be void!
                
                // ISampleEx.cs should have agrint and CString
                string iSampleExCs = File.ReadAllText(Path.Combine(outputDir, "ISampleEx.cs"));
                Assert.Contains("agrint MethodExOne(const agrint& value);", iSampleExCs);
                Assert.Contains("CString MethodExTwo();", iSampleExCs);
                Assert.DoesNotContain("void MethodExOne", iSampleExCs); // Should NOT be void!
                Assert.DoesNotContain("void MethodExTwo", iSampleExCs); // Should NOT be void!
                
                // ISampleEx2.cs should have bool, agrint, and void
                string iSampleEx2Cs = File.ReadAllText(Path.Combine(outputDir, "ISampleEx2.cs"));
                Assert.Contains("bool MethodEx2One();", iSampleEx2Cs);
                Assert.Contains("agrint MethodEx2Two(const bool& flag);", iSampleEx2Cs);
                Assert.Contains("void MethodEx2Three();", iSampleEx2Cs);
                Assert.DoesNotContain("void MethodEx2One", iSampleEx2Cs); // Should NOT be void!
                Assert.DoesNotContain("void MethodEx2Two", iSampleEx2Cs); // Should NOT be void!

                // Also verify no cross-contamination of methods
                Assert.DoesNotContain("MethodExOne", iSampleCs); // ISample should not have ISampleEx methods
                Assert.DoesNotContain("MethodOne", iSampleExCs); // ISampleEx should not have ISample methods
                Assert.DoesNotContain("MethodEx2", iSampleCs); // ISample should not have ISampleEx2 methods
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void MultipleInterfaceFiles_WithImplementingClasses_ShouldPreserveReturnTypes()
        {
            // Arrange - Create interfaces with implementing classes to test the full pipeline
            string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);

            try
            {
                // ISample.h
                string iSampleHeader = @"
#pragma once

class __declspec(dllexport) ISample
{
public:
    virtual ~ISample(){};
    static ISample* GetInstance();

    virtual void VoidMethod() = 0;
    virtual bool BoolMethod() = 0;
};
";
                File.WriteAllText(Path.Combine(tempDir, "ISample.h"), iSampleHeader);

                // CSample.h - Implementing class
                string cSampleHeader = @"
#pragma once
#include ""ISample.h""

class CSample : public ISample
{
public:
    CSample();
    void VoidMethod();
    bool BoolMethod();
};
";
                File.WriteAllText(Path.Combine(tempDir, "CSample.h"), cSampleHeader);

                // CSample.cpp
                string cSampleCpp = @"
#include ""CSample.h""

ISample* ISample::GetInstance()
{
    return new CSample();
}

CSample::CSample()
{
}

void CSample::VoidMethod()
{
    // Implementation
}

bool CSample::BoolMethod()
{
    return true;
}
";
                File.WriteAllText(Path.Combine(tempDir, "CSample.cpp"), cSampleCpp);

                // ISampleEx.h
                string iSampleExHeader = @"
#pragma once

class __declspec(dllexport) ISampleEx
{
public:
    virtual ~ISampleEx(){};
    static ISampleEx* GetInstance();

    virtual agrint AgrintMethod() = 0;
    virtual CString CStringMethod() = 0;
};
";
                File.WriteAllText(Path.Combine(tempDir, "ISampleEx.h"), iSampleExHeader);

                // CSampleEx.h
                string cSampleExHeader = @"
#pragma once
#include ""ISampleEx.h""

class CSampleEx : public ISampleEx
{
public:
    CSampleEx();
    agrint AgrintMethod();
    CString CStringMethod();
};
";
                File.WriteAllText(Path.Combine(tempDir, "CSampleEx.h"), cSampleExHeader);

                // CSampleEx.cpp
                string cSampleExCpp = @"
#include ""CSampleEx.h""

ISampleEx* ISampleEx::GetInstance()
{
    return new CSampleEx();
}

CSampleEx::CSampleEx()
{
}

agrint CSampleEx::AgrintMethod()
{
    return 42;
}

CString CSampleEx::CStringMethod()
{
    return _T(""test"");
}
";
                File.WriteAllText(Path.Combine(tempDir, "CSampleEx.cpp"), cSampleExCpp);

                string outputDir = Path.Combine(tempDir, "Generated_CS");

                // Act
                var api = new CppToCsConverterApi();
                api.ConvertDirectory(tempDir, outputDir);

                // Assert - Check both interfaces preserve their return types
                string iSampleCs = File.ReadAllText(Path.Combine(outputDir, "ISample.cs"));
                Assert.Contains("void VoidMethod();", iSampleCs);
                Assert.Contains("bool BoolMethod();", iSampleCs);
                Assert.DoesNotContain("void BoolMethod", iSampleCs);

                string iSampleExCs = File.ReadAllText(Path.Combine(outputDir, "ISampleEx.cs"));
                Assert.Contains("agrint AgrintMethod();", iSampleExCs);
                Assert.Contains("CString CStringMethod();", iSampleExCs);
                Assert.DoesNotContain("void AgrintMethod", iSampleExCs);
                Assert.DoesNotContain("void CStringMethod", iSampleExCs);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }
    }
}
