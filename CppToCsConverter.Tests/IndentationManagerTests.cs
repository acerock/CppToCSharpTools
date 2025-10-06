using System.Collections.Generic;
using Xunit;
using CppToCsConverter.Core.Utils;

namespace CppToCsConverter.Tests
{
    /// <summary>
    /// Tests for IndentationManager to ensure proper comment and method body indentation
    /// based on C# structure nesting and original C++ indentation context.
    /// </summary>
    public class IndentationManagerTests
    {
        [Fact]
        public void DetectOriginalIndentation_EmptyString_ReturnsZero()
        {
            // Arrange
            var input = "";

            // Act
            var result = IndentationManager.DetectOriginalIndentation(input);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void DetectOriginalIndentation_NoIndentation_ReturnsZero()
        {
            // Arrange
            var input = "// Comment at start of line\nint value = 42;";

            // Act
            var result = IndentationManager.DetectOriginalIndentation(input);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void DetectOriginalIndentation_FourSpaces_ReturnsFour()
        {
            // Arrange
            var input = "    // Comment with 4 space indentation\n    int value = 42;";

            // Act
            var result = IndentationManager.DetectOriginalIndentation(input);

            // Assert
            Assert.Equal(4, result);
        }

        [Fact]
        public void DetectOriginalIndentation_EightSpaces_ReturnsEight()
        {
            // Arrange
            var input = "        // Comment with 8 space indentation\n        int value = 42;";

            // Act
            var result = IndentationManager.DetectOriginalIndentation(input);

            // Assert
            Assert.Equal(8, result);
        }

        [Fact]
        public void DetectOriginalIndentation_TabsConvertedToSpaces_ReturnsCorrectValue()
        {
            // Arrange
            var input = "\t// Comment with tab indentation";

            // Act
            var result = IndentationManager.DetectOriginalIndentation(input);

            // Assert
            Assert.Equal(4, result); // 1 tab = 4 spaces
        }

        [Fact]
        public void DetectOriginalIndentation_MixedTabsAndSpaces_ReturnsCorrectValue()
        {
            // Arrange
            var input = "\t  // Comment with tab + 2 spaces";

            // Act
            var result = IndentationManager.DetectOriginalIndentation(input);

            // Assert
            Assert.Equal(6, result); // 1 tab (4 spaces) + 2 spaces = 6
        }

        [Fact]
        public void DetectOriginalIndentation_SkipsEmptyLines_UsesFirstNonEmptyLine()
        {
            // Arrange
            var input = "\n\n        // First non-empty line with 8 spaces\n    // Another line";

            // Act
            var result = IndentationManager.DetectOriginalIndentation(input);

            // Assert
            Assert.Equal(8, result);
        }

        [Fact]
        public void ReindentBlock_ClassLevelComment_ProducesCorrectIndentation()
        {
            // Arrange
            var originalComment = "    // Class comment from header\n    // Second line of comment";
            var originalIndentation = 4; // From C++ header at class level
            var targetLevel = IndentationManager.Levels.Class; // C# class level

            // Act
            var result = IndentationManager.ReindentBlock(originalComment, originalIndentation, targetLevel);

            // Assert
            var expected = "    // Class comment from header\n    // Second line of comment";
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ReindentBlock_MethodLevelComment_ProducesCorrectIndentation()
        {
            // Arrange
            var originalComment = "        // Method comment from header\n        // Second line of comment";
            var originalIndentation = 8; // From C++ header at method level
            var targetLevel = IndentationManager.Levels.ClassMember; // C# method declaration level

            // Act
            var result = IndentationManager.ReindentBlock(originalComment, originalIndentation, targetLevel);

            // Assert
            var expected = "        // Method comment from header\n        // Second line of comment";
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ReindentBlock_MethodBody_ProducesCorrectIndentation()
        {
            // Arrange
            var originalBody = "    if (condition)\n    {\n        doSomething();\n    }";
            var originalIndentation = 4; // From C++ source file  
            var targetLevel = IndentationManager.Levels.MethodBody; // C# method body level

            // Act
            var result = IndentationManager.ReindentBlock(originalBody, originalIndentation, targetLevel);

            // Assert
            var expected = "            if (condition)\n            {\n                doSomething();\n            }";
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ReindentBlock_PreservesRelativeIndentation()
        {
            // Arrange - Original has nested structure
            var originalBody = "    if (condition)\n    {\n        for (int i = 0; i < 10; i++)\n        {\n            doSomething(i);\n        }\n    }";
            var originalIndentation = 4;
            var targetLevel = IndentationManager.Levels.MethodBody;

            // Act
            var result = IndentationManager.ReindentBlock(originalBody, originalIndentation, targetLevel);

            // Assert - Should preserve the 4-space nesting within the method body
            var expected = "            if (condition)\n            {\n                for (int i = 0; i < 10; i++)\n                {\n                    doSomething(i);\n                }\n            }";
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ReindentBlock_EmptyLines_HandledCorrectly()
        {
            // Arrange
            var originalBody = "    // Comment\n\n    int value = 42;\n\n    return value;";
            var originalIndentation = 4;
            var targetLevel = IndentationManager.Levels.MethodBody;

            // Act
            var result = IndentationManager.ReindentBlock(originalBody, originalIndentation, targetLevel);

            // Assert - Empty lines should get target indentation
            var expected = "            // Comment\n            \n            int value = 42;\n            \n            return value;";
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ReindentMethodComments_ProducesCorrectFormat()
        {
            // Arrange
            var comments = new List<string>
            {
                "    // Method does something important",
                "    // @param value The input value", 
                "    // @return The processed result"
            };
            var originalIndentation = 4;

            // Act
            var result = IndentationManager.ReindentMethodComments(comments, originalIndentation);

            // Assert
            var expected = "        // Method does something important\n        // @param value The input value\n        // @return The processed result";
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ReindentMethodBody_ProducesCorrectFormat()
        {
            // Arrange
            var methodBody = "    m_value = value;\n    return m_value * 2;";
            var originalIndentation = 4;

            // Act
            var result = IndentationManager.ReindentMethodBody(methodBody, originalIndentation);

            // Assert
            var expected = "            m_value = value;\n            return m_value * 2;";
            Assert.Equal(expected, result);
        }

        [Fact]
        public void IsOneLinerMethod_SingleLineWithBraces_ReturnsTrue()
        {
            // Arrange
            var methodBody = "{ return m_value; }";

            // Act
            var result = IndentationManager.IsOneLinerMethod(methodBody);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsOneLinerMethod_SingleLineWithSpaces_ReturnsTrue()
        {
            // Arrange
            var methodBody = "  { return m_value; }  ";

            // Act
            var result = IndentationManager.IsOneLinerMethod(methodBody);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsOneLinerMethod_MultipleLines_ReturnsFalse()
        {
            // Arrange
            var methodBody = "{\n    return m_value;\n}";

            // Act
            var result = IndentationManager.IsOneLinerMethod(methodBody);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsOneLinerMethod_EmptyString_ReturnsFalse()
        {
            // Arrange
            var methodBody = "";

            // Act
            var result = IndentationManager.IsOneLinerMethod(methodBody);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetIndentationForLevel_ReturnsCorrectSpacing()
        {
            // Assert
            Assert.Equal("", IndentationManager.GetIndentationForLevel(IndentationManager.Levels.Namespace));
            Assert.Equal("    ", IndentationManager.GetIndentationForLevel(IndentationManager.Levels.Class));
            Assert.Equal("        ", IndentationManager.GetIndentationForLevel(IndentationManager.Levels.ClassMember));
            Assert.Equal("            ", IndentationManager.GetIndentationForLevel(IndentationManager.Levels.MethodBody));
        }

        [Fact]
        public void ReindentBlock_ZeroIndentationToMethodBody_HandlesCorrectly()
        {
            // Arrange - C++ code at start of line (no indentation)
            var originalBody = "if (condition)\n{\n    doSomething();\n}";
            var originalIndentation = 0;
            var targetLevel = IndentationManager.Levels.MethodBody;

            // Act
            var result = IndentationManager.ReindentBlock(originalBody, originalIndentation, targetLevel);

            // Assert
            var expected = "            if (condition)\n            {\n                doSomething();\n            }";
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("// Single line comment", 0, IndentationManager.Levels.ClassMember, "        // Single line comment")]
        [InlineData("    /* Multi-line\n       comment */", 4, IndentationManager.Levels.ClassMember, "        /* Multi-line\n           comment */")]
        [InlineData("        void Method();", 8, IndentationManager.Levels.ClassMember, "        void Method();")]
        public void ReindentBlock_VariousInputs_ProducesExpectedOutput(string input, int originalIndent, int targetLevel, string expected)
        {
            // Act
            var result = IndentationManager.ReindentBlock(input, originalIndent, targetLevel);

            // Assert
            Assert.Equal(expected, result);
        }
    }
}