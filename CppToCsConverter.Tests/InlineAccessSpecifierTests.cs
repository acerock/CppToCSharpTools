using System;
using System.IO;
using System.Linq;
using CppToCsConverter.Core.Parsers;
using Xunit;

namespace CppToCsConverter.Tests
{
    /// <summary>
    /// Tests for handling inline access specifiers (e.g., "protected: int memberTwo;")
    /// based on CSample.h requirements
    /// </summary>
    public class InlineAccessSpecifierTests
    {
        private readonly CppHeaderParser _headerParser;

        public InlineAccessSpecifierTests()
        {
            _headerParser = new CppHeaderParser();
        }

        [Fact]
        public void ParseHeaderFile_InlineProtectedMember_ShouldParseCorrectly()
        {
            // Arrange - Test from CSample.h line 40
            var headerContent = @"
class CSomeClass
{
    StructOne memberOne;
    protected: int memberTwo;
    
    public: CSomeClass() : memberTwo(33) {
        memberOne.lTestType = 0;
    }
};";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, headerContent);

            try
            {
                // Act
                var classes = _headerParser.ParseHeaderFile(tempFile);

                // Assert
                Assert.Single(classes);
                var cppClass = classes[0];
                Assert.Equal("CSomeClass", cppClass.Name);
                
                // Check members
                Assert.Equal(2, cppClass.Members.Count);
                
                // First member should be private (default)
                var memberOne = cppClass.Members.FirstOrDefault(m => m.Name == "memberOne");
                Assert.NotNull(memberOne);
                Assert.Equal(Core.Models.AccessSpecifier.Private, memberOne.AccessSpecifier);
                Assert.Equal("StructOne", memberOne.Type);
                
                // Second member should be protected (inline specifier)
                var memberTwo = cppClass.Members.FirstOrDefault(m => m.Name == "memberTwo");
                Assert.NotNull(memberTwo);
                Assert.Equal(Core.Models.AccessSpecifier.Protected, memberTwo.AccessSpecifier);
                Assert.Equal("int", memberTwo.Type);
                
                // Constructor should be public (inline specifier)
                var constructor = cppClass.Methods.FirstOrDefault(m => m.IsConstructor);
                Assert.NotNull(constructor);
                Assert.Equal(Core.Models.AccessSpecifier.Public, constructor.AccessSpecifier);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseHeaderFile_MixedInlineAndStandaloneAccessSpecifiers_ShouldParseCorrectly()
        {
            // Arrange - Mix of inline and standalone access specifiers
            var headerContent = @"
class TestClass
{
    int defaultPrivate;
    protected: int inlineProtected;
    private: int inlinePrivate;
    
public:
    int standalonePublic;
    public: int anotherInlinePublic;
    
private:
    void PrivateMethod();
};";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, headerContent);

            try
            {
                // Act
                var classes = _headerParser.ParseHeaderFile(tempFile);

                // Assert
                Assert.Single(classes);
                var cppClass = classes[0];
                
                // Check all members have correct access specifiers
                Assert.Equal(Core.Models.AccessSpecifier.Private, 
                    cppClass.Members.First(m => m.Name == "defaultPrivate").AccessSpecifier);
                
                Assert.Equal(Core.Models.AccessSpecifier.Protected, 
                    cppClass.Members.First(m => m.Name == "inlineProtected").AccessSpecifier);
                
                Assert.Equal(Core.Models.AccessSpecifier.Private, 
                    cppClass.Members.First(m => m.Name == "inlinePrivate").AccessSpecifier);
                
                Assert.Equal(Core.Models.AccessSpecifier.Public, 
                    cppClass.Members.First(m => m.Name == "standalonePublic").AccessSpecifier);
                
                Assert.Equal(Core.Models.AccessSpecifier.Public, 
                    cppClass.Members.First(m => m.Name == "anotherInlinePublic").AccessSpecifier);
                
                Assert.Equal(Core.Models.AccessSpecifier.Private, 
                    cppClass.Methods.First(m => m.Name == "PrivateMethod").AccessSpecifier);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseHeaderFile_InlineAccessSpecifierWithConstructor_ShouldParseCorrectly()
        {
            // Arrange - Constructor with inline public access specifier
            var headerContent = @"
class TestClass
{
    int m_value;
    
    public: TestClass() : m_value(0) {
    }
};";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, headerContent);

            try
            {
                // Act
                var classes = _headerParser.ParseHeaderFile(tempFile);

                // Assert
                Assert.Single(classes);
                var cppClass = classes[0];
                
                // Member should be private (default)
                var member = cppClass.Members.FirstOrDefault(m => m.Name == "m_value");
                Assert.NotNull(member);
                Assert.Equal(Core.Models.AccessSpecifier.Private, member.AccessSpecifier);
                
                // Constructor should be public (inline specifier)
                var constructor = cppClass.Methods.FirstOrDefault(m => m.IsConstructor);
                Assert.NotNull(constructor);
                Assert.Equal(Core.Models.AccessSpecifier.Public, constructor.AccessSpecifier);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseHeaderFile_InlineAccessSpecifierWithMethod_ShouldParseCorrectly()
        {
            // Arrange - Method with inline access specifier
            var headerContent = @"
class TestClass
{
private:
    int m_value;
    
    public: int GetValue() const;
    protected: void SetValue(int value);
};";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, headerContent);

            try
            {
                // Act
                var classes = _headerParser.ParseHeaderFile(tempFile);

                // Assert
                Assert.Single(classes);
                var cppClass = classes[0];
                
                // Check method access specifiers
                var getValue = cppClass.Methods.FirstOrDefault(m => m.Name == "GetValue");
                Assert.NotNull(getValue);
                Assert.Equal(Core.Models.AccessSpecifier.Public, getValue.AccessSpecifier);
                
                var setValue = cppClass.Methods.FirstOrDefault(m => m.Name == "SetValue");
                Assert.NotNull(setValue);
                Assert.Equal(Core.Models.AccessSpecifier.Protected, setValue.AccessSpecifier);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }
    }
}
