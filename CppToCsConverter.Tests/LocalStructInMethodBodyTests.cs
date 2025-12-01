using System;
using System.IO;
using Xunit;
using CppToCsConverter.Core.Core;

namespace CppToCsConverter.Tests
{
    public class LocalStructInMethodBodyTests
    {
        [Fact]
        public void MethodBody_WithLocalStructDefinition_ShouldPreserveStructAsIs()
        {
            // Arrange
            var headerContent = @"
class CAgrLibHS
{
public:
    agrint DistributeAnalysis();
};";

            var sourceContent = @"
agrint CAgrLibHS::DistributeAnalysis()
{
    struct DistrStruct
    {
    public:
        TAttId attId;
        TDimValue dimVal;
        CString cDimFlag;
        TAttId attDistrId;
        TDimValue dimDistr;
        TDimValue dimOrig;
        DistrStruct(){;};
    };

    DistrStruct as[7];

    // Some more code
    return 0;
}";

            var tempDir = Path.Combine(Path.GetTempPath(), "LocalStructTest_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            var headerFile = Path.Combine(tempDir, "CAgrLibHS.h");
            var sourceFile = Path.Combine(tempDir, "CAgrLibHS.cpp");
            File.WriteAllText(headerFile, headerContent);
            File.WriteAllText(sourceFile, sourceContent);

            try
            {
                // Act
                var converter = new CppToCsStructuralConverter();
                converter.ConvertDirectory(tempDir, tempDir);

                // Assert
                var outputFile = Path.Combine(tempDir, "CAgrLibHS.cs");
                Assert.True(File.Exists(outputFile));
                
                var generatedContent = File.ReadAllText(outputFile);
                
                // Should preserve the entire struct definition in method body
                Assert.Contains("struct DistrStruct", generatedContent);
                Assert.Contains("public:", generatedContent);
                Assert.Contains("TAttId attId;", generatedContent);
                Assert.Contains("TDimValue dimVal;", generatedContent);
                Assert.Contains("CString cDimFlag;", generatedContent);
                Assert.Contains("DistrStruct(){;};", generatedContent);
                Assert.Contains("DistrStruct as[7];", generatedContent);
                
                // Should NOT create a separate top-level DistrStruct class
                Assert.DoesNotContain("internal class DistrStruct", generatedContent);
                
                // Should NOT extract the struct constructor as a local method
                Assert.DoesNotContain("private static void DistrStruct()", generatedContent);
                
                // The struct should be inside the method body
                var methodStart = generatedContent.IndexOf("public agrint DistributeAnalysis()");
                var structPos = generatedContent.IndexOf("struct DistrStruct");
                Assert.True(structPos > methodStart, "Struct should appear after method declaration");
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void MethodBody_WithMultipleLocalStructs_ShouldPreserveAllAsIs()
        {
            // Arrange
            var headerContent = @"
class CTest
{
public:
    void TestMethod();
};";

            var sourceContent = @"
void CTest::TestMethod()
{
    struct LocalStruct1
    {
        int value;
    };

    struct LocalStruct2
    {
        double amount;
    };

    LocalStruct1 s1;
    LocalStruct2 s2;
}";

            var tempDir = Path.Combine(Path.GetTempPath(), "MultiLocalStructTest_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            var headerFile = Path.Combine(tempDir, "CTest.h");
            var sourceFile = Path.Combine(tempDir, "CTest.cpp");
            File.WriteAllText(headerFile, headerContent);
            File.WriteAllText(sourceFile, sourceContent);

            try
            {
                // Act
                var converter = new CppToCsStructuralConverter();
                converter.ConvertDirectory(tempDir, tempDir);

                // Assert
                var outputFile = Path.Combine(tempDir, "CTest.cs");
                Assert.True(File.Exists(outputFile));
                
                var generatedContent = File.ReadAllText(outputFile);
                
                // Should preserve both struct definitions in method body
                Assert.Contains("struct LocalStruct1", generatedContent);
                Assert.Contains("struct LocalStruct2", generatedContent);
                
                // Should NOT create separate top-level classes
                Assert.DoesNotContain("internal class LocalStruct1", generatedContent);
                Assert.DoesNotContain("internal class LocalStruct2", generatedContent);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }
    }
}
