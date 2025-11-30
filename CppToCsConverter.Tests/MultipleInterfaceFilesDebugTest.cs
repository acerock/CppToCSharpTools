using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;
using CppToCsConverter.Core;

namespace CppToCsConverter.Tests
{
    public class MultipleInterfaceFilesDebugTest
    {
        private readonly ITestOutputHelper _output;

        public MultipleInterfaceFilesDebugTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void DebugMultipleInterfaceFiles_ShowGeneratedContent()
        {
            // Arrange - Create 3 interface files similar to real scenario: ISample, ISampleEx, ISampleEx2
            string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);

            try
            {
                // ISample.h - void and bool methods
                string iSampleHeader = @"
#pragma once

class __declspec(dllexport) ISample
{
public:
    virtual ~ISample(){};
    static ISample* GetInstance();

    virtual void VoidMethodInISample() = 0;
    virtual bool BoolMethodInISample() = 0;
};
";
                File.WriteAllText(Path.Combine(tempDir, "ISample.h"), iSampleHeader);

                // ISampleEx.h - agrint and CString methods
                string iSampleExHeader = @"
#pragma once

class __declspec(dllexport) ISampleEx
{
public:
    virtual ~ISampleEx(){};
    static ISampleEx* GetInstance();

    virtual agrint AgrintMethodInISampleEx() = 0;
    virtual CString CStringMethodInISampleEx() = 0;
};
";
                File.WriteAllText(Path.Combine(tempDir, "ISampleEx.h"), iSampleExHeader);

                // ISampleEx2.h - mixed return types
                string iSampleEx2Header = @"
#pragma once

class __declspec(dllexport) ISampleEx2
{
public:
    virtual ~ISampleEx2(){};
    static ISampleEx2* GetInstance();

    virtual bool BoolMethodInISampleEx2() = 0;
    virtual agrint AgrintMethodInISampleEx2() = 0;
    virtual void VoidMethodInISampleEx2() = 0;
};
";
                File.WriteAllText(Path.Combine(tempDir, "ISampleEx2.h"), iSampleEx2Header);

                string outputDir = Path.Combine(tempDir, "Generated_CS");

                // Act
                var api = new CppToCsConverterApi();
                api.ConvertDirectory(tempDir, outputDir);

                // Debug Output - Show what was actually generated
                _output.WriteLine("==================== ISample.cs ====================");
                string iSampleCs = File.ReadAllText(Path.Combine(outputDir, "ISample.cs"));
                _output.WriteLine(iSampleCs);

                _output.WriteLine("\n==================== ISampleEx.cs ====================");
                string iSampleExCs = File.ReadAllText(Path.Combine(outputDir, "ISampleEx.cs"));
                _output.WriteLine(iSampleExCs);

                _output.WriteLine("\n==================== ISampleEx2.cs ====================");
                string iSampleEx2Cs = File.ReadAllText(Path.Combine(outputDir, "ISampleEx2.cs"));
                _output.WriteLine(iSampleEx2Cs);

                // Assert - Verify each interface has correct return types
                
                // ISample should have void and bool (NOT all void!)
                Assert.Contains("void VoidMethodInISample();", iSampleCs);
                Assert.Contains("bool BoolMethodInISample();", iSampleCs);
                Assert.DoesNotContain("void BoolMethodInISample", iSampleCs);
                
                // ISampleEx should have agrint and CString (NOT all void!)
                Assert.Contains("agrint AgrintMethodInISampleEx();", iSampleExCs);
                Assert.Contains("CString CStringMethodInISampleEx();", iSampleExCs);
                Assert.DoesNotContain("void AgrintMethodInISampleEx", iSampleExCs);
                Assert.DoesNotContain("void CStringMethodInISampleEx", iSampleExCs);
                
                // ISampleEx2 should have bool, agrint, and void
                Assert.Contains("bool BoolMethodInISampleEx2();", iSampleEx2Cs);
                Assert.Contains("agrint AgrintMethodInISampleEx2();", iSampleEx2Cs);
                Assert.Contains("void VoidMethodInISampleEx2();", iSampleEx2Cs);
                Assert.DoesNotContain("void BoolMethodInISampleEx2", iSampleEx2Cs);
                Assert.DoesNotContain("void AgrintMethodInISampleEx2", iSampleEx2Cs);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }
    }
}
