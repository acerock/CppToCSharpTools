using System.IO;
using System.Linq;
using CppToCsConverter.Core;
using CppToCsConverter.Core.Parsers;
using Xunit;

namespace CppToCsConverter.Tests
{
    /// <summary>
    /// Tests for struct handling based on readme.md requirements.
    /// Ensures structs are copied as-is without transformation and comments are preserved.
    /// </summary>
    public class StructHandlingTests
    {
        private readonly CppHeaderParser _headerParser;

        public StructHandlingTests()
        {
            _headerParser = new CppHeaderParser();
        }

        [Fact]
        public void ParseStructs_SimpleStruct_ShouldParseCorrectly()
        {
            // Arrange - Simple struct pattern from readme
            var headerContent = @"
struct MyStruct
{
    bool MyBoolField;
    agrint MyIntField;
};";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, headerContent);

            try
            {
                // Act
                var classes = _headerParser.ParseHeaderFile(tempFile);
                var structs = classes.Where(c => c.IsStruct).ToList();

                // Assert
                Assert.Single(structs);
                var cppStruct = structs[0];
                Assert.Equal("MyStruct", cppStruct.Name);
                Assert.True(cppStruct.IsStruct);
                // Verify struct members are parsed
                Assert.Equal(2, cppStruct.Members.Count);
                Assert.Contains(cppStruct.Members, m => m.Name == "MyBoolField");
                Assert.Contains(cppStruct.Members, m => m.Name == "MyIntField");
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseStructs_TypedefStruct_ShouldParseCorrectly()
        {
            // Arrange - Typedef struct pattern from readme
            var headerContent = @"
typedef struct
{
    bool MyBoolField;
    agrint MyIntField;
} MyStruct;";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, headerContent);

            try
            {
                // Act
                var classes = _headerParser.ParseHeaderFile(tempFile);
                var structs = classes.Where(c => c.IsStruct).ToList();

                // Assert
                Assert.Single(structs);
                var cppStruct = structs[0];
                Assert.Equal("MyStruct", cppStruct.Name);
                Assert.True(cppStruct.IsStruct);
                // Verify struct members are parsed
                Assert.Equal(2, cppStruct.Members.Count);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseStructs_TypedefStructTag_ShouldParseCorrectly()
        {
            // Arrange - Typedef struct tag pattern from readme
            var headerContent = @"
typedef struct MyTag
{
    // This struct has a comment copied as is
    bool someBool;
    agrint intValue;
} MyOtherStruct;";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, headerContent);

            try
            {
                // Act
                var classes = _headerParser.ParseHeaderFile(tempFile);
                var structs = classes.Where(c => c.IsStruct).ToList();

                // Assert
                Assert.Single(structs);
                var cppStruct = structs[0];
                Assert.Equal("MyOtherStruct", cppStruct.Name);
                Assert.True(cppStruct.IsStruct);
                // Verify comment on member is preserved
                Assert.Contains(cppStruct.Members, m => m.PrecedingComments.Any(c => c.Contains("This struct has a comment copied as is")));
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseStructs_WithPrecedingComments_ShouldPreserveComments()
        {
            // Arrange - Struct with preceding comments from readme
            var headerContent = @"
/* My struct */
typedef struct
{
    bool MyBoolField;
    agrint MyIntField;
} MyStruct;";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, headerContent);

            try
            {
                // Act
                var classes = _headerParser.ParseHeaderFile(tempFile);
                var structs = classes.Where(c => c.IsStruct).ToList();

                // Assert
                Assert.Single(structs);
                var cppStruct = structs[0];
                Assert.Equal("MyStruct", cppStruct.Name);
                Assert.Single(cppStruct.PrecedingComments);
                Assert.Contains("My struct", cppStruct.PrecedingComments[0]);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseStructs_MultipleStructsInFile_ShouldParseAll()
        {
            // Arrange - Multiple structs like in readme example
            var headerContent = @"
/* My struct */
typedef struct
{
    bool MyBoolField;
    agrint MyIntField;
} MyStruct;

struct SimpleStruct
{
    bool SimpleBoolField;
    int SimpleIntField;
};

// This comment is for the other struct
typedef struct MyTag
{
    // This struct has a comment copied as is
    bool someBool;
    agrint intValue;
} MyOtherStruct;";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, headerContent);

            try
            {
                // Act
                var classes = _headerParser.ParseHeaderFile(tempFile);
                var structs = classes.Where(c => c.IsStruct).ToList();

                // Assert
                Assert.Equal(3, structs.Count);
                
                // First struct
                var firstStruct = structs.FirstOrDefault(s => s.Name == "MyStruct");
                Assert.NotNull(firstStruct);
                Assert.True(firstStruct.IsStruct);
                Assert.Single(firstStruct.PrecedingComments);
                
                // Second struct
                var secondStruct = structs.FirstOrDefault(s => s.Name == "SimpleStruct");
                Assert.NotNull(secondStruct);
                Assert.True(secondStruct.IsStruct);
                
                // Third struct
                var thirdStruct = structs.FirstOrDefault(s => s.Name == "MyOtherStruct");
                Assert.NotNull(thirdStruct);
                Assert.True(thirdStruct.IsStruct);
                Assert.Single(thirdStruct.PrecedingComments);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseStructs_DoesNotDuplicateAsClasses_ShouldOnlyParseAsStructs()
        {
            // Arrange - Ensure structs don't get parsed as classes too
            var headerContent = @"
struct MyStruct
{
    bool field1;
    int field2;
};";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, headerContent);

            try
            {
                // Act
                var classes = _headerParser.ParseHeaderFile(tempFile);
                var structs = classes.Where(c => c.IsStruct).ToList();

                // Assert
                Assert.Single(structs);
                Assert.Equal("MyStruct", structs[0].Name);
                
                // With unified approach, structs are in the classes list with IsStruct=true
                var structAsClass = classes.FirstOrDefault(c => c.Name == "MyStruct");
                Assert.NotNull(structAsClass);
                Assert.True(structAsClass.IsStruct, "MyStruct should be marked as a struct");
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseAndGenerateStruct_WithRegionsAccessSpecifiersAndConstructor_ShouldGenerateCorrectly()
        {
            // Arrange - Struct with regions, access specifiers, and constructor
            var headerContent = @"
struct StructOne
{
protected:
    agrint lTestType;

#pragma region Just a h-file pragma test
public:

    // att-id member comment
    TAttId attId;
    TDimValue dimVal;
#pragma endregion // Comment test

public:
    StructOne(const TAttid& inAttId, const TDimValue &inDimVal, agrint lInTestType=0)
    {
        lTestType = lInTestType;
        attid = inAttId;
        dimVal = inDimVal;
    }    
};";

            var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var outputDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);
            Directory.CreateDirectory(outputDir);
            
            var tempHeaderFile = Path.Combine(tempDir, "StructOne.h");
            File.WriteAllText(tempHeaderFile, headerContent);

            try
            {
                // Act - Parse and generate
                var converter = new CppToCsConverterApi();
                converter.ConvertSpecificFiles(
                    tempDir, 
                    new[] { "StructOne.h" }, 
                    outputDir);

                // Assert - Check generated file
                var generatedFile = Path.Combine(outputDir, "StructOne.cs");
                Assert.True(File.Exists(generatedFile), "Generated C# file should exist");
                
                var generatedContent = File.ReadAllText(generatedFile);
                
                // Verify it's generated as internal class (C++ structs become C# classes)
                Assert.Contains("internal class StructOne", generatedContent);
                
                // Verify protected member
                Assert.Contains("protected agrint lTestType;", generatedContent);
                
                // Verify regions are commented out with //#region and //#endregion
                Assert.Contains("//#region Just a h-file pragma test", generatedContent);
                Assert.Contains("//#endregion", generatedContent);
                Assert.Contains("// Comment test", generatedContent);
                
                // Verify public members with comment
                Assert.Contains("// att-id member comment", generatedContent);
                Assert.Contains("public TAttId attId;", generatedContent);
                Assert.Contains("public TDimValue dimVal;", generatedContent);
                
                // Verify constructor is preserved with correct signature and body
                // Note: spacing around & is normalized in output (both "TAttid& " and "TDimValue& " are acceptable)
                Assert.Contains("public StructOne(const TAttid& inAttId, const TDimValue& inDimVal, agrint lInTestType = 0)", generatedContent);
                Assert.Contains("lTestType = lInTestType;", generatedContent);
                Assert.Contains("attid = inAttId;", generatedContent);
                Assert.Contains("dimVal = inDimVal;", generatedContent);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
                if (Directory.Exists(outputDir))
                {
                    Directory.Delete(outputDir, true);
                }
            }
        }
    }
}