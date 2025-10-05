using System.IO;
using System.Linq;
using Xunit;
using CppToCsConverter.Core.Parsers;

namespace CppToCsConverter.Tests
{
    /// <summary>
    /// Tests for comment and region preservation based on readme.md examples.
    /// Covers header comments, source comments, region handling, and comment association.
    /// </summary>
    public class CommentAndRegionTests
    {
        private readonly CppHeaderParser _headerParser;
        private readonly CppSourceParser _sourceParser;

        public CommentAndRegionTests()
        {
            _headerParser = new CppHeaderParser();
            _sourceParser = new CppSourceParser();
        }

        [Fact]
        public void ParseHeaderFile_CommentBeforeClass_ShouldAssociateWithClass()
        {
            // Arrange - Based on readme.md class comment example
            var headerContent = @"
// This is a sample class
class CSample : public ISample
{
private:
    agrint m_value;
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
                Assert.Equal("CSample", cppClass.Name);
                Assert.Single(cppClass.PrecedingComments);
                Assert.Contains("// This is a sample class", cppClass.PrecedingComments[0]);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseHeaderFile_MultilineCommentBeforeClass_ShouldPreserveStructure()
        {
            // Arrange - Based on readme.md advanced comment example
            var headerContent = @"
/* 
 *  Some description goes here
    And it ends like this */

// We actually have more to comment

/* And more */
class CSample : public ISample
{
private:
    agrint m_value;
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
                Assert.Equal("CSample", cppClass.Name);
                Assert.True(cppClass.PrecedingComments.Count >= 3); // Should capture all comment blocks
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseHeaderFile_CommentBeforeMember_ShouldAssociateWithMember()
        {
            // Arrange - Based on readme.md member comment example
            var headerContent = @"
class CSample : public ISample
{
private:

    // My value holder

    agrint m_value;
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
                Assert.Single(cppClass.Members);
                
                var member = cppClass.Members[0];
                Assert.Equal("m_value", member.Name);
                Assert.Single(member.PrecedingComments);
                Assert.Contains("// My value holder", member.PrecedingComments[0]);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseHeaderFile_CommentBeforeMethod_ShouldAssociateWithMethod()
        {
            // Arrange - Based on readme.md method comment example
            var headerContent = @"
class CSample : public ISample
{
private:
    agrint m_value;

    /* Here is a test method
     * We describe stuff here
       still inside comment
    */ 
    void MethodSample(const CString & cStr);
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
                
                var method = cppClass.Methods.FirstOrDefault(m => m.Name == "MethodSample");
                Assert.NotNull(method);
                Assert.True(method.HeaderComments.Count > 0);
                Assert.Contains("Here is a test method", string.Join(" ", method.HeaderComments));
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseSourceFile_CommentBeforeImplementation_ShouldAssociate()
        {
            // Arrange - Based on readme.md source comment example
            var sourceContent = @"
// For now we just log
void CSample::MethodSample(const CString & cStr)
{
    AGRWriteLog(cStr);
}";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, sourceContent);

            try
            {
                // Act
                var (methods, staticInits) = _sourceParser.ParseSourceFile(tempFile);

                // Assert
                var method = methods.FirstOrDefault(m => m.Name == "MethodSample");
                Assert.NotNull(method);
                Assert.Single(method.SourceComments);
                Assert.Contains("// For now we just log", method.SourceComments[0]);
                Assert.Contains("AGRWriteLog(cStr);", method.ImplementationBody);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseHeaderFile_RegionMarkers_ShouldConvertToComments()
        {
            // Arrange - Based on readme.md region in header example
            var headerContent = @"
class CSample : public ISample
{
private:
#pragma region My Variables

    // My comment
    agrint m_value;

#pragma endregion // My Variables

public:
    void MethodSample(const CString & cStr);
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
                
                // Should have converted pragma regions to comments
                var member = cppClass.Members.FirstOrDefault(m => m.Name == "m_value");
                Assert.NotNull(member);
                
                // Check if region start is captured
                Assert.NotEmpty(member.RegionStart);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseSourceFile_RegionMarkers_ShouldPreserveAsRegions()
        {
            // Arrange - Based on readme.md region in source example
            var sourceContent = @"
void CSample::MethodSample(const CString & cStr)
{
    AGRWriteLog(cStr);
}

#pragma region More Samples

void CSample::MethodSample2(const CString & cStr)
{
    AGRWriteLog(cStr);
}

void CSample::MethodSample3(const CString & cStr)
{
    AGRWriteLog(cStr);
}

#pragma endregion
";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, sourceContent);

            try
            {
                // Act
                var (methods, staticInits) = _sourceParser.ParseSourceFile(tempFile);

                // Assert
                Assert.Equal(3, methods.Count);
                
                // Check if region is associated with methods
                var method2 = methods.FirstOrDefault(m => m.Name == "MethodSample2");
                Assert.NotNull(method2);
                
                // Region should be captured in the parsing
                Assert.NotEmpty(method2.SourceRegionStart);
                Assert.Contains("More Samples", method2.SourceRegionStart);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseSourceFile_RegionWithDescription_ShouldPreserveDescription()
        {
            // Arrange - Test region with description
            var sourceContent = @"
#pragma region My Nice Region

void CSample::TestMethod()
{
    // Implementation
}

#pragma endregion // My Nice Region
";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, sourceContent);

            try
            {
                // Act
                var (methods, staticInits) = _sourceParser.ParseSourceFile(tempFile);

                // Assert
                var method = methods.FirstOrDefault(m => m.Name == "TestMethod");
                Assert.NotNull(method);
                Assert.Contains("My Nice Region", method.SourceRegionStart);
                Assert.Contains("My Nice Region", method.SourceRegionEnd);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ParseHeaderFile_EmptyLinesInComments_ShouldPreserve()
        {
            // Arrange - Test comment blocks with empty lines
            var headerContent = @"
// First comment

// Second comment after empty line
class TestClass
{
    // Method comment

    // More method comments
    void TestMethod();
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
                Assert.True(cppClass.PrecedingComments.Count >= 2);
                
                var method = cppClass.Methods.FirstOrDefault(m => m.Name == "TestMethod");
                Assert.NotNull(method);
                Assert.True(method.HeaderComments.Count >= 2);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }
    }
}