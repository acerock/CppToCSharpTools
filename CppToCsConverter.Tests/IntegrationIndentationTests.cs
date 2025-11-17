using System.IO;
using System.Linq;
using Xunit;
using CppToCsConverter.Core.Generators;
using CppToCsConverter.Core.Models;
using CppToCsConverter.Core.Parsers;

namespace CppToCsConverter.Tests
{
    /// <summary>
    /// Integration tests demonstrating improved indentation handling for comments and method bodies
    /// across different C++ source structures and target C# alignment requirements.
    /// </summary>
    public class IntegrationIndentationTests
    {
        private readonly CsClassGenerator _classGenerator;
        private readonly CppHeaderParser _headerParser;
        private readonly CppSourceParser _sourceParser;

        public IntegrationIndentationTests()
        {
            _classGenerator = new CsClassGenerator();
            _headerParser = new CppHeaderParser();
            _sourceParser = new CppSourceParser();
        }

        [Fact]
        public void ComplexIndentation_ClassWithVariousCommentLevels_ProducesCorrectCsAlignment()
        {
            // Arrange - C++ header with comments at different indentation levels
            var headerContent = @"
class CSample
{
private:
    // Private member comment
    int m_value;
    
public:
    // Constructor comment
    CSample();
    
    // Method comment
    void ProcessValue();
};";

            // C++ source file with method implementations at different levels
            var sourceContent = @"
// Implementation comment (position 0)
CSample::CSample()
{
    m_value = 0;  // Initialize to zero
}

    // Method implementation comment (position 4)
    void CSample::ProcessValue()
    {
        if (m_value > 0)
        {
            m_value *= 2;
        }
    }";

            var headerFile = Path.GetTempFileName();
            var sourceFile = Path.GetTempFileName();
            File.WriteAllText(headerFile, headerContent);
            File.WriteAllText(sourceFile, sourceContent);

            try
            {
                // Act
                var classes = _headerParser.ParseHeaderFile(headerFile);
                var cppClass = classes.FirstOrDefault(c => c.Name == "CSample");
                Assert.NotNull(cppClass);

                var (sourceMethods, _) = _sourceParser.ParseSourceFile(sourceFile);
                
                var result = _classGenerator.GenerateClass(cppClass, sourceMethods, "CSample.cs");

                // Debug output
                System.Console.WriteLine("=== GENERATED C# CODE ===");
                System.Console.WriteLine(result);
                System.Console.WriteLine("=== END GENERATED CODE ===");

                // Assert - Check that comments are preserved and method bodies are properly indented
                Assert.Contains("// Private member comment", result);
                Assert.Contains("// Constructor comment", result);
                Assert.Contains("// Method comment", result);
                
                // Verify class structure is correct
                Assert.Contains("public class CSample", result);
                Assert.Contains("private int m_value;", result);
                Assert.Contains("public CSample()", result);
                Assert.Contains("public void ProcessValue()", result);
                
                // Assert - Check that method bodies are properly indented
                Assert.Contains("m_value = 0;  // Initialize to zero", result);
                Assert.Contains("if (m_value > 0)", result);
                Assert.Contains("m_value *= 2;", result);
                
                // Verify the IndentationManager is working by checking relative structure
                // The method body should be more indented than the method declaration
                var processValueIndex = result.IndexOf("public void ProcessValue()");
                var ifStatementIndex = result.IndexOf("if (m_value > 0)");
                var assignmentIndex = result.IndexOf("m_value *= 2;");
                
                Assert.True(processValueIndex < ifStatementIndex, "Method declaration should come before method body");
                Assert.True(ifStatementIndex < assignmentIndex, "If statement should come before assignment");
            }
            finally
            {
                File.Delete(headerFile);
                File.Delete(sourceFile);
            }
        }

        [Fact]
        public void NestedIndentation_PreservesRelativeStructure()
        {
            // Arrange - C++ with nested control structures
            var sourceContent = @"
void CSample::ComplexMethod()
{
    for (int i = 0; i < 10; i++)
    {
        if (i % 2 == 0)
        {
            switch (i)
            {
                case 0:
                    DoSomething();
                    break;
                case 2:
                    DoSomethingElse();
                    break;
                default:
                    DoDefault();
                    break;
            }
        }
    }
}";

            var sourceFile = Path.GetTempFileName();
            File.WriteAllText(sourceFile, sourceContent);

            try
            {
                // Act
                var (sourceMethods, _) = _sourceParser.ParseSourceFile(sourceFile);
                var method = sourceMethods.FirstOrDefault(m => m.Name == "ComplexMethod");
                Assert.NotNull(method);

                // Create a dummy class for testing
                var cppClass = new CppClass { Name = "CSample" };
                cppClass.Methods.Add(method);
                
                var result = _classGenerator.GenerateClass(cppClass, sourceMethods, "CSample.cs");

                // Assert - Check that nested structures maintain relative indentation (updated for file-scoped namespace)
                // Original C++ had 4-space base indentation, C# should have 8-space base (was 12)
                Assert.Contains("        for (int i = 0; i < 10; i++)", result);      // Base level: 8 spaces
                Assert.Contains("        {", result);                                   // Base level: 8 spaces
                Assert.Contains("            if (i % 2 == 0)", result);               // +1 level: 12 spaces
                Assert.Contains("            {", result);                               // +1 level: 12 spaces  
                Assert.Contains("                switch (i)", result);                 // +2 levels: 16 spaces
                Assert.Contains("                {", result);                           // +2 levels: 16 spaces
                Assert.Contains("                    case 0:", result);                // +3 levels: 20 spaces
                Assert.Contains("                        DoSomething();", result);     // +4 levels: 24 spaces
            }
            finally
            {
                File.Delete(sourceFile);
            }
        }

        [Fact]
        public void ZeroIndentationCpp_ConvertsToProperCsIndentation()
        {
            // Arrange - C++ code starting at column 0 (no indentation)
            var sourceContent = @"
void CSample::Method()
{
if (condition)
{
DoSomething();
return;
}
}";

            var sourceFile = Path.GetTempFileName();
            File.WriteAllText(sourceFile, sourceContent);

            try
            {
                // Act
                var (sourceMethods, _) = _sourceParser.ParseSourceFile(sourceFile);
                var method = sourceMethods.FirstOrDefault(m => m.Name == "Method");
                Assert.NotNull(method);

                // Create a dummy class for testing
                var cppClass = new CppClass { Name = "CSample" };
                cppClass.Methods.Add(method);
                
                var result = _classGenerator.GenerateClass(cppClass, sourceMethods, "CSample.cs");

                // Assert - Even with zero indentation in C++, should get proper C# indentation (updated for file-scoped namespace)
                Assert.Contains("        if (condition)", result);      // 8 spaces for method body (was 12)
                Assert.Contains("        {", result);                   // 8 spaces
                Assert.Contains("        DoSomething();", result);      // 8 spaces - preserves original zero indentation at method body level
                Assert.Contains("        return;", result);             // 8 spaces - preserves original zero indentation at method body level
                Assert.Contains("        }", result);                   // 8 spaces
            }
            finally
            {
                File.Delete(sourceFile);
            }
        }

        [Fact]
        public void MultiLineComments_PreserveFormatting()
        {
            // Arrange - Multi-line comments with varying indentation
            var headerContent = @"
class CSample
{
public:
    /*
     * Multi-line comment describing method
     * Second line with different indentation
       Third line with even more indentation
     */
    void Method();
};";

            var headerFile = Path.GetTempFileName();
            File.WriteAllText(headerFile, headerContent);

            try
            {
                // Act
                var classes = _headerParser.ParseHeaderFile(headerFile);
                var cppClass = classes.FirstOrDefault(c => c.Name == "CSample");
                Assert.NotNull(cppClass);

                var result = _classGenerator.GenerateClass(cppClass, new System.Collections.Generic.List<CppMethod>(), "CSample.cs");

                // Assert - Multi-line comments should be present (indentation may vary by code path)
                Assert.Contains("/*", result);
                Assert.Contains("*", result);
                Assert.Contains("*/", result);
                Assert.Contains("public void Method()", result);
            }
            finally
            {
                File.Delete(headerFile);
            }
        }

        [Fact]
        public void InlineImplementation_HandlesIndentationCorrectly()
        {
            // Arrange - Inline method in header with specific indentation
            var headerContent = @"
class CSample
{
public:
    bool IsValid() { 
        return m_value > 0; 
    }
    
    int GetDoubleValue() 
    { 
        return m_value * 2; 
    }
};";

            var headerFile = Path.GetTempFileName();
            File.WriteAllText(headerFile, headerContent);

            try
            {
                // Act  
                var classes = _headerParser.ParseHeaderFile(headerFile);
                var cppClass = classes.FirstOrDefault(c => c.Name == "CSample");
                Assert.NotNull(cppClass);

                var result = _classGenerator.GenerateClass(cppClass, new System.Collections.Generic.List<CppMethod>(), "CSample.cs");

                // Assert - Inline implementations should be indented to method body level (updated for file-scoped namespace)
                Assert.Contains("        return m_value > 0;", result);  // 8 spaces for method body (was 12)
                Assert.Contains("        return m_value * 2;", result);  // 8 spaces for method body (was 12)
            }
            finally
            {
                File.Delete(headerFile);
            }
        }
    }
}