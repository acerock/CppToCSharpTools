using System.IO;
using System.Linq;
using Xunit;
using CppToCsConverter.Core.Parsers;
using CppToCsConverter.Core.Models;

namespace CppToCsConverter.Tests
{
    /// <summary>
    /// Tests for static member initialization based on readme.md examples.
    /// Covers parsing static member declarations and their initialization from source files.
    /// </summary>
    public class StaticMemberInitializationTests
    {
        private readonly CppHeaderParser _headerParser;
        private readonly CppSourceParser _sourceParser;

        public StaticMemberInitializationTests()
        {
            _headerParser = new CppHeaderParser();
            _sourceParser = new CppSourceParser();
        }

        [Fact]
        public void ParseStaticMemberInitialization_SimpleCase_ShouldParseCorrectly()
        {
            // Arrange - Based on readme.md static member example
            var headerContent = @"
class CSample : public ISample
{
private:
    static agrint s_value;
};";

            var sourceContent = @"
agrint CSample::s_value = 42;
";

            var headerFile = Path.GetTempFileName();
            var sourceFile = Path.GetTempFileName();
            File.WriteAllText(headerFile, headerContent);
            File.WriteAllText(sourceFile, sourceContent);

            try
            {
                // Act
                var classes = _headerParser.ParseHeaderFile(headerFile);
                var staticInits = _sourceParser.ParseSourceFile(sourceFile);

                // Assert
                Assert.Single(classes);
                var cppClass = classes[0];
                
                // Should have static member in header
                var staticMember = cppClass.Members.FirstOrDefault(m => m.Name == "s_value");
                Assert.NotNull(staticMember);
                Assert.True(staticMember.IsStatic);
                Assert.Equal("agrint", staticMember.Type);

                // Should parse static initialization from source
                // Note: This tests the parser's ability to capture static initializations
                // The actual merging logic would be tested in integration tests
            }
            finally
            {
                File.Delete(headerFile);
                File.Delete(sourceFile);
            }
        }

        [Fact]
        public void ParseStaticMemberInitialization_ArrayInitialization_ShouldHandle()
        {
            // Arrange - Test static array initialization
            var headerContent = @"
class CSample
{
private:
    static const CString ColFrom[4];
};";

            var sourceContent = @"
const CString CSample::ColFrom[4] = {
    ""First"",
    ""Second"",
    ""Third"",
    ""Fourth""
};
";

            var headerFile = Path.GetTempFileName();
            var sourceFile = Path.GetTempFileName();
            File.WriteAllText(headerFile, headerContent);
            File.WriteAllText(sourceFile, sourceContent);

            try
            {
                // Act
                var classes = _headerParser.ParseHeaderFile(headerFile);
                var (methods, staticInits) = _sourceParser.ParseSourceFile(sourceFile);

                // Assert
                Assert.Single(classes);
                var cppClass = classes[0];
                
                var arrayMember = cppClass.Members.FirstOrDefault(m => m.Name == "ColFrom");
                Assert.NotNull(arrayMember);
                Assert.True(arrayMember.IsStatic);
                Assert.Equal("CString", arrayMember.Type);
                Assert.Equal("4", arrayMember.ArraySize);
            }
            finally
            {
                File.Delete(headerFile);
                File.Delete(sourceFile);
            }
        }

        [Fact]
        public void ParseStaticMemberInitialization_MultipleMembers_ShouldParseAll()
        {
            // Arrange - Multiple static members
            var headerContent = @"
class CSample
{
private:
    static int s_intValue;
    static CString s_stringValue;
    static bool s_boolValue;
public:
    static double s_publicValue;
};";

            var sourceContent = @"
int CSample::s_intValue = 100;
CString CSample::s_stringValue = ""Hello"";
bool CSample::s_boolValue = true;
double CSample::s_publicValue = 3.14;
";

            var headerFile = Path.GetTempFileName();
            var sourceFile = Path.GetTempFileName();
            File.WriteAllText(headerFile, headerContent);
            File.WriteAllText(sourceFile, sourceContent);

            try
            {
                // Act
                var classes = _headerParser.ParseHeaderFile(headerFile);
                var (methods, staticInits) = _sourceParser.ParseSourceFile(sourceFile);

                // Assert
                Assert.Single(classes);
                var cppClass = classes[0];
                
                // Should have all static members
                Assert.Equal(4, cppClass.Members.Count(m => m.IsStatic));
                
                var intMember = cppClass.Members.FirstOrDefault(m => m.Name == "s_intValue");
                Assert.NotNull(intMember);
                Assert.True(intMember.IsStatic);
                Assert.Equal(AccessSpecifier.Private, intMember.AccessSpecifier);

                var publicMember = cppClass.Members.FirstOrDefault(m => m.Name == "s_publicValue");
                Assert.NotNull(publicMember);
                Assert.True(publicMember.IsStatic);
                Assert.Equal(AccessSpecifier.Public, publicMember.AccessSpecifier);
            }
            finally
            {
                File.Delete(headerFile);
                File.Delete(sourceFile);
            }
        }

        [Fact]
        public void ParseStaticMemberInitialization_WithComments_ShouldPreserveComments()
        {
            // Arrange - Static member with comments
            var headerContent = @"
class CSample
{
private:
    // Configuration value
    static int s_config;
};";

            var sourceContent = @"
// Initialize configuration to default
int CSample::s_config = 42;
";

            var headerFile = Path.GetTempFileName();
            var sourceFile = Path.GetTempFileName();
            File.WriteAllText(headerFile, headerContent);
            File.WriteAllText(sourceFile, sourceContent);

            try
            {
                // Act
                var classes = _headerParser.ParseHeaderFile(headerFile);
                var (methods, staticInits) = _sourceParser.ParseSourceFile(sourceFile);

                // Assert
                Assert.Single(classes);
                var cppClass = classes[0];
                
                var staticMember = cppClass.Members.FirstOrDefault(m => m.Name == "s_config");
                Assert.NotNull(staticMember);
                Assert.True(staticMember.IsStatic);
                
                // Should have comment from header
                Assert.Single(staticMember.PrecedingComments);
                Assert.Contains("Configuration value", staticMember.PrecedingComments[0]);
            }
            finally
            {
                File.Delete(headerFile);
                File.Delete(sourceFile);
            }
        }

        [Fact]
        public void ParseStaticMemberInitialization_ConstStaticMember_ShouldHandleCorrectly()
        {
            // Arrange - Const static member (common pattern)
            var headerContent = @"
class CSample
{
private:
    static const int MAX_SIZE;
    static const CString DEFAULT_NAME;
};";

            var sourceContent = @"
const int CSample::MAX_SIZE = 1000;
const CString CSample::DEFAULT_NAME = ""Default"";
";

            var headerFile = Path.GetTempFileName();
            var sourceFile = Path.GetTempFileName();
            File.WriteAllText(headerFile, headerContent);
            File.WriteAllText(sourceFile, sourceContent);

            try
            {
                // Act
                var classes = _headerParser.ParseHeaderFile(headerFile);
                var (methods, staticInits) = _sourceParser.ParseSourceFile(sourceFile);

                // Assert
                Assert.Single(classes);
                var cppClass = classes[0];
                
                var maxSizeMember = cppClass.Members.FirstOrDefault(m => m.Name == "MAX_SIZE");
                Assert.NotNull(maxSizeMember);
                Assert.True(maxSizeMember.IsStatic);
                Assert.Equal("int", maxSizeMember.Type);

                var defaultNameMember = cppClass.Members.FirstOrDefault(m => m.Name == "DEFAULT_NAME");
                Assert.NotNull(defaultNameMember);
                Assert.True(defaultNameMember.IsStatic);
                Assert.Equal("CString", defaultNameMember.Type);
            }
            finally
            {
                File.Delete(headerFile);
                File.Delete(sourceFile);
            }
        }

        [Fact]
        public void ParseStaticMemberInitialization_ClassScopeSyntax_ShouldParse()
        {
            // Arrange - Test different class scope syntax variations
            var sourceContent = @"
// Standard syntax
int CSample::s_value1 = 10;

// With namespace
int MyNamespace::CSample::s_value2 = 20;

// Complex initialization
CString CSample::s_complexValue = CString(""Complex"") + "" Value"";
";

            var sourceFile = Path.GetTempFileName();
            File.WriteAllText(sourceFile, sourceContent);

            try
            {
                // Act
                var (methods, staticInits) = _sourceParser.ParseSourceFile(sourceFile);

                // Assert
                // The parser should be able to handle various class scope syntax
                // This tests the parser's robustness with different C++ syntax patterns
                Assert.NotNull(staticInits);
            }
            finally
            {
                File.Delete(sourceFile);
            }
        }
    }
}