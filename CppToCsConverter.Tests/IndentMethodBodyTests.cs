using CppToCsConverter.Core.Core;
using System.Text;

namespace CppToCsConverter.Tests;

public class IndentMethodBodyTests
{
    [Fact]
    public void IndentMethodBody_SimpleOneLineMethod_ShouldAddIndentationOnly()
    {
        // Arrange
        var converter = new CppToCsStructuralConverter();
        var methodBody = "return true;";
        var indentation = "        "; // 8 spaces
        
        // Act
        var result = InvokeIndentMethodBody(converter, methodBody, indentation);
        
        // Assert
        Assert.Equal("        return true;", result);
    }
    
    [Fact]
    public void IndentMethodBody_MultiLineWithOriginalIndentation_ShouldPreserveStructure()
    {
        // Arrange
        var converter = new CppToCsStructuralConverter();
        var methodBody = "if (condition)\n    return true;\nreturn false;";
        var indentation = "        "; // 8 spaces
        
        // Act
        var result = InvokeIndentMethodBody(converter, methodBody, indentation);
        
        // Assert
        var expected = "        if (condition)\n            return true;\n        return false;";
        Assert.Equal(expected, result);
    }
    
    [Fact]
    public void IndentMethodBody_WithBlankLines_ShouldPreserveBlankLines()
    {
        // Arrange  
        var converter = new CppToCsStructuralConverter();
        var methodBody = "int x = 0;\n \nreturn x;";
        var indentation = "        "; // 8 spaces
        
        // Act
        var result = InvokeIndentMethodBody(converter, methodBody, indentation);
        
        // Assert
        var lines = result.Split('\n');
        Assert.Equal(3, lines.Length);
        Assert.Equal("        int x = 0;", lines[0]);
        Assert.Equal("        ", lines[1]); // Should preserve blank line
        Assert.Equal("        return x;", lines[2]);
    }
    
    [Fact]
    public void IndentMethodBody_InlineFromHeader_ShouldIndentProperly()
    {
        // Arrange - Simulating what we extract from header inline methods (after trimming)
        var converter = new CppToCsStructuralConverter();
        var methodBody = "\t\tif (dim1.IsEmpty()) \n\t\t\treturn 0;\n\t\t\n\t\treturn 42;"; // Trimmed typical extraction from header
        var indentation = "            "; // 12 spaces for inline methods
        
        // Act
        var result = InvokeIndentMethodBody(converter, methodBody, indentation);
        
        // Assert
        var lines = result.Split('\n');
        Assert.Equal(4, lines.Length);
        Assert.Contains("if (dim1.IsEmpty())", lines[0]);
        Assert.Contains("return 0;", lines[1]);
        Assert.Equal("            ", lines[2]); // Blank line with indentation
        Assert.Contains("return 42;", lines[3]);
        
        // Should not start or end with just whitespace
        Assert.DoesNotMatch(@"^\s*$", lines[0]); // First line should have content
        Assert.DoesNotMatch(@"^\s*$", lines[3]); // Last line should have content
    }
    
    [Fact]
    public void IndentMethodBody_FromCppFile_ShouldPreserveExactStructure()
    {
        // Arrange - Simulating what we extract from .cpp files  
        var converter = new CppToCsStructuralConverter();
        var methodBody = "m_value1 = 0;\n \ncValue1 = _T(\"ABC\");\ncValue2 = _T(\"DEF\");";
        var indentation = "        "; // 8 spaces for .cpp methods
        
        // Act
        var result = InvokeIndentMethodBody(converter, methodBody, indentation);
        
        // Assert
        var lines = result.Split('\n');
        Assert.Equal(4, lines.Length);
        Assert.Equal("        m_value1 = 0;", lines[0]);
        Assert.Equal("        ", lines[1]); // Preserved blank line
        Assert.Equal("        cValue1 = _T(\"ABC\");", lines[2]);
        Assert.Equal("        cValue2 = _T(\"DEF\");", lines[3]);
    }
    
    // Helper method to invoke the private IndentMethodBody method
    private static string InvokeIndentMethodBody(CppToCsStructuralConverter converter, string methodBody, string indentation)
    {
        var method = typeof(CppToCsStructuralConverter).GetMethod("IndentMethodBody", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        return (string)method!.Invoke(converter, new object[] { methodBody, indentation })!;
    }
}