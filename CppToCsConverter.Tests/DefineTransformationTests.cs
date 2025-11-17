using System;
using CppToCsConverter.Core.Models;
using Xunit;

namespace CppToCsConverter.Tests
{
    /// <summary>
    /// Tests for CppDefine transformation based on readme.md requirements.
    /// Validates type inference and value normalization for #define to const conversion.
    /// </summary>
    public class DefineTransformationTests
    {
        [Theory]
        [InlineData("_T('')", "char")]
        [InlineData("_T( '\\t' )", "char")]
        [InlineData("'t'", "char")]
        [InlineData("'\\n'", "char")]
        public void InferType_CharacterLiterals_ReturnsChar(string value, string expectedType)
        {
            // Arrange
            var define = new CppDefine { Value = value };

            // Act
            var actualType = define.InferType();

            // Assert
            Assert.Equal(expectedType, actualType);
        }

        [Theory]
        [InlineData("_(\"\")", "string")]
        [InlineData("_(\"A\")", "string")]
        [InlineData("_(\"ABC\")", "string")]
        [InlineData("_T(\"\")", "string")]
        [InlineData("_T(\"test\")", "string")]
        [InlineData("\"hello\"", "string")]
        public void InferType_StringLiterals_ReturnsString(string value, string expectedType)
        {
            // Arrange
            var define = new CppDefine { Value = value };

            // Act
            var actualType = define.InferType();

            // Assert
            Assert.Equal(expectedType, actualType);
        }

        [Theory]
        [InlineData("0", "int")]
        [InlineData("123", "int")]
        [InlineData("-456", "int")]
        [InlineData("1", "int")]
        [InlineData("2", "int")]
        public void InferType_IntegerLiterals_ReturnsInt(string value, string expectedType)
        {
            // Arrange
            var define = new CppDefine { Value = value };

            // Act
            var actualType = define.InferType();

            // Assert
            Assert.Equal(expectedType, actualType);
        }

        [Theory]
        [InlineData("0L", "long")]
        [InlineData("-123456789L", "long")]
        [InlineData("999999999l", "long")]
        public void InferType_LongLiterals_ReturnsLong(string value, string expectedType)
        {
            // Arrange
            var define = new CppDefine { Value = value };

            // Act
            var actualType = define.InferType();

            // Assert
            Assert.Equal(expectedType, actualType);
        }

        [Theory]
        [InlineData("0.0005", "double")]
        [InlineData("-0.1", "double")]
        [InlineData("1D", "double")]
        [InlineData("1d", "double")]
        [InlineData("3.14159", "double")]
        public void InferType_DoubleLiterals_ReturnsDouble(string value, string expectedType)
        {
            // Arrange
            var define = new CppDefine { Value = value };

            // Act
            var actualType = define.InferType();

            // Assert
            Assert.Equal(expectedType, actualType);
        }

        [Theory]
        [InlineData("TRUE", "bool")]
        [InlineData("YES", "bool")]
        [InlineData("OK", "bool")]
        [InlineData("true", "bool")]
        [InlineData("FALSE", "bool")]
        [InlineData("NO", "bool")]
        [InlineData("NOTOK", "bool")]
        [InlineData("false", "bool")]
        public void InferType_BooleanLiterals_ReturnsBool(string value, string expectedType)
        {
            // Arrange
            var define = new CppDefine { Value = value };

            // Act
            var actualType = define.InferType();

            // Assert
            Assert.Equal(expectedType, actualType);
        }

        [Theory]
        [InlineData("_T('')", "''")]
        [InlineData("_T( '\\t' )", "'\\t'")]
        [InlineData("'t'", "'t'")]
        public void NormalizeValue_CharacterLiterals_RemovesMacros(string value, string expectedNormalized)
        {
            // Arrange
            var define = new CppDefine { Value = value };

            // Act
            var actualNormalized = define.NormalizeValue();

            // Assert
            Assert.Equal(expectedNormalized, actualNormalized);
        }

        [Theory]
        [InlineData("_(\"\")", "\"\"")]
        [InlineData("_(\"A\")", "\"A\"")]
        [InlineData("_(\"ABC\")", "\"ABC\"")]
        [InlineData("_T(\"test\")", "\"test\"")]
        [InlineData("\"hello\"", "\"hello\"")]
        public void NormalizeValue_StringLiterals_RemovesMacros(string value, string expectedNormalized)
        {
            // Arrange
            var define = new CppDefine { Value = value };

            // Act
            var actualNormalized = define.NormalizeValue();

            // Assert
            Assert.Equal(expectedNormalized, actualNormalized);
        }

        [Theory]
        [InlineData("TRUE", "true")]
        [InlineData("YES", "true")]
        [InlineData("OK", "true")]
        [InlineData("FALSE", "false")]
        [InlineData("NO", "false")]
        [InlineData("NOTOK", "false")]
        public void NormalizeValue_BooleanLiterals_ConvertsToLowercase(string value, string expectedNormalized)
        {
            // Arrange
            var define = new CppDefine { Value = value };

            // Act
            var actualNormalized = define.NormalizeValue();

            // Assert
            Assert.Equal(expectedNormalized, actualNormalized);
        }

        [Theory]
        [InlineData("123", "123")]
        [InlineData("0L", "0L")]
        [InlineData("3.14159", "3.14159")]
        public void NormalizeValue_NumericLiterals_ReturnsAsIs(string value, string expectedNormalized)
        {
            // Arrange
            var define = new CppDefine { Value = value };

            // Act
            var actualNormalized = define.NormalizeValue();

            // Assert
            Assert.Equal(expectedNormalized, actualNormalized);
        }

        [Fact]
        public void GetAccessModifier_HeaderDefine_ReturnsInternal()
        {
            // Arrange
            var define = new CppDefine { IsFromHeader = true };

            // Act
            var modifier = define.GetAccessModifier();

            // Assert
            Assert.Equal("internal", modifier);
        }

        [Fact]
        public void GetAccessModifier_SourceDefine_ReturnsPrivate()
        {
            // Arrange
            var define = new CppDefine { IsFromHeader = false };

            // Act
            var modifier = define.GetAccessModifier();

            // Assert
            Assert.Equal("private", modifier);
        }

        [Fact]
        public void ToCSharpConst_HeaderDefineWithInt_GeneratesCorrectDeclaration()
        {
            // Arrange - Based on readme.md example
            var define = new CppDefine
            {
                Name = "WARNING",
                Value = "1",
                IsFromHeader = true
            };

            // Act
            var csharpConst = define.ToCSharpConst();

            // Assert
            Assert.Equal("internal const int WARNING = 1;", csharpConst);
        }

        [Fact]
        public void ToCSharpConst_SourceDefineWithInt_GeneratesCorrectDeclaration()
        {
            // Arrange - Based on readme.md example
            var define = new CppDefine
            {
                Name = "CPP_DEFINE",
                Value = "10",
                IsFromHeader = false
            };

            // Act
            var csharpConst = define.ToCSharpConst();

            // Assert
            Assert.Equal("private const int CPP_DEFINE = 10;", csharpConst);
        }

        [Fact]
        public void ToCSharpConst_CharDefine_GeneratesCorrectDeclaration()
        {
            // Arrange
            var define = new CppDefine
            {
                Name = "mychar2define",
                Value = "_T( '\\t' )",
                IsFromHeader = true
            };

            // Act
            var csharpConst = define.ToCSharpConst();

            // Assert
            Assert.Equal("internal const char mychar2define = '\\t';", csharpConst);
        }

        [Fact]
        public void ToCSharpConst_StringDefine_GeneratesCorrectDeclaration()
        {
            // Arrange
            var define = new CppDefine
            {
                Name = "mystr3define",
                Value = "_(\"ABC\")",
                IsFromHeader = true
            };

            // Act
            var csharpConst = define.ToCSharpConst();

            // Assert
            Assert.Equal("internal const string mystr3define = \"ABC\";", csharpConst);
        }

        [Fact]
        public void ToCSharpConst_LongDefine_GeneratesCorrectDeclaration()
        {
            // Arrange
            var define = new CppDefine
            {
                Name = "mylong2define",
                Value = "-123456789L",
                IsFromHeader = true
            };

            // Act
            var csharpConst = define.ToCSharpConst();

            // Assert
            Assert.Equal("internal const long mylong2define = -123456789L;", csharpConst);
        }

        [Fact]
        public void ToCSharpConst_DoubleDefine_GeneratesCorrectDeclaration()
        {
            // Arrange
            var define = new CppDefine
            {
                Name = "mydouble1define",
                Value = "0.0005",
                IsFromHeader = true
            };

            // Act
            var csharpConst = define.ToCSharpConst();

            // Assert
            Assert.Equal("internal const double mydouble1define = 0.0005;", csharpConst);
        }

        [Fact]
        public void ToCSharpConst_BoolDefine_GeneratesCorrectDeclaration()
        {
            // Arrange
            var define = new CppDefine
            {
                Name = "mybool1define",
                Value = "TRUE",
                IsFromHeader = true
            };

            // Act
            var csharpConst = define.ToCSharpConst();

            // Assert
            Assert.Equal("internal const bool mybool1define = true;", csharpConst);
        }
    }
}
