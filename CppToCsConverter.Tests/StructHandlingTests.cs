using System.IO;
using System.Linq;
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
                var structs = _headerParser.ParseStructsFromHeaderFile(tempFile);

                // Assert
                Assert.Single(structs);
                var cppStruct = structs[0];
                Assert.Equal("MyStruct", cppStruct.Name);
                Assert.Equal(Core.Models.StructType.Simple, cppStruct.Type);
                Assert.Contains("struct MyStruct", cppStruct.OriginalDefinition);
                Assert.Contains("bool MyBoolField;", cppStruct.OriginalDefinition);
                Assert.Contains("agrint MyIntField;", cppStruct.OriginalDefinition);
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
                var structs = _headerParser.ParseStructsFromHeaderFile(tempFile);

                // Assert
                Assert.Single(structs);
                var cppStruct = structs[0];
                Assert.Equal("MyStruct", cppStruct.Name);
                Assert.Equal(Core.Models.StructType.Typedef, cppStruct.Type);
                Assert.Contains("typedef struct", cppStruct.OriginalDefinition);
                Assert.Contains("} MyStruct;", cppStruct.OriginalDefinition);
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
                var structs = _headerParser.ParseStructsFromHeaderFile(tempFile);

                // Assert
                Assert.Single(structs);
                var cppStruct = structs[0];
                Assert.Equal("MyOtherStruct", cppStruct.Name);
                Assert.Equal(Core.Models.StructType.TypedefTag, cppStruct.Type);
                Assert.Contains("typedef struct MyTag", cppStruct.OriginalDefinition);
                Assert.Contains("// This struct has a comment copied as is", cppStruct.OriginalDefinition);
                Assert.Contains("} MyOtherStruct;", cppStruct.OriginalDefinition);
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
                var structs = _headerParser.ParseStructsFromHeaderFile(tempFile);

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
                var structs = _headerParser.ParseStructsFromHeaderFile(tempFile);

                // Assert
                Assert.Equal(3, structs.Count);
                
                // First struct
                var firstStruct = structs.FirstOrDefault(s => s.Name == "MyStruct");
                Assert.NotNull(firstStruct);
                Assert.Equal(Core.Models.StructType.Typedef, firstStruct.Type);
                Assert.Single(firstStruct.PrecedingComments);
                
                // Second struct
                var secondStruct = structs.FirstOrDefault(s => s.Name == "SimpleStruct");
                Assert.NotNull(secondStruct);
                Assert.Equal(Core.Models.StructType.Simple, secondStruct.Type);
                
                // Third struct
                var thirdStruct = structs.FirstOrDefault(s => s.Name == "MyOtherStruct");
                Assert.NotNull(thirdStruct);
                Assert.Equal(Core.Models.StructType.TypedefTag, thirdStruct.Type);
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
                var structs = _headerParser.ParseStructsFromHeaderFile(tempFile);
                var classes = _headerParser.ParseHeaderFile(tempFile);

                // Assert
                Assert.Single(structs);
                Assert.Equal("MyStruct", structs[0].Name);
                
                // Should not be parsed as a class
                Assert.DoesNotContain(classes, c => c.Name == "MyStruct");
            }
            finally
            {
                File.Delete(tempFile);
            }
        }
    }
}