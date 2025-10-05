using System;
using Xunit;
using CppToCsConverter.Core.Generators;

namespace CppToCsConverter.Tests
{
    /// <summary>
    /// Tests for C++ to C# type conversion based on readme.md examples.
    /// </summary>
    public class TypeConverterTests
    {
        private readonly TypeConverter _converter;

        public TypeConverterTests()
        {
            _converter = new TypeConverter();
        }

        [Fact]
        public void ConvertType_BasicTypes_ShouldMapCorrectly()
        {
            // Arrange & Act & Assert
            Assert.Equal("bool", _converter.ConvertType("bool"));
            Assert.Equal("int", _converter.ConvertType("int"));
            Assert.Equal("string", _converter.ConvertType("CString"));
            Assert.Equal("int", _converter.ConvertType("agrint"));
        }

        [Fact]
        public void ConvertType_WithConstModifier_ShouldRemoveConst()
        {
            // Arrange & Act & Assert - Based on readme examples
            Assert.Equal("string", _converter.ConvertType("const CString"));
            Assert.Equal("TDimValue", _converter.ConvertType("const TDimValue"));
            Assert.Equal("bool", _converter.ConvertType("const bool"));
            Assert.Equal("int", _converter.ConvertType("const agrint"));
        }

        [Fact]
        public void ConvertType_WithReferenceAndPointer_ShouldRemoveModifiers()
        {
            // Arrange & Act & Assert - Based on readme parameter examples
            Assert.Equal("string", _converter.ConvertType("const CString&"));
            Assert.Equal("TDimValue", _converter.ConvertType("const TDimValue&"));
            Assert.Equal("bool", _converter.ConvertType("const bool&"));
            Assert.Equal("string", _converter.ConvertType("CString*"));
        }

        [Fact]
        public void ConvertDefaultValue_CommonCppDefaults_ShouldConvertToCS()
        {
            // Arrange & Act & Assert - Based on readme default parameter examples
            Assert.Equal("null", _converter.ConvertDefaultValue("0"));
            Assert.Equal("null", _converter.ConvertDefaultValue("nullptr"));
            Assert.Equal("false", _converter.ConvertDefaultValue("false"));
            Assert.Equal("true", _converter.ConvertDefaultValue("true"));
            Assert.Equal("string.Empty", _converter.ConvertDefaultValue("\"\""));
        }

        [Fact]
        public void ConvertDefaultValue_NumericValues_ShouldPreserve()
        {
            // Arrange & Act & Assert
            Assert.Equal("42", _converter.ConvertDefaultValue("42"));
            Assert.Equal("3.14", _converter.ConvertDefaultValue("3.14"));
            Assert.Equal("-1", _converter.ConvertDefaultValue("-1"));
        }

        [Theory]
        [InlineData("std::string", "std::string")]
        [InlineData("std::vector<int>", "std::vector<int>")]
        [InlineData("DWORD", "uint")]
        [InlineData("LPSTR", "string")]
        public void ConvertType_CommonCppTypes_ShouldPreserveStdTypes(string cppType, string expectedCsType)
        {
            // Act
            var result = _converter.ConvertType(cppType);
            
            // Assert
            Assert.Equal(expectedCsType, result);
        }

        [Fact]
        public void ConvertType_CustomTypes_ShouldPreserve()
        {
            // Arrange & Act & Assert - Based on readme examples with TDimValue
            Assert.Equal("TDimValue", _converter.ConvertType("TDimValue"));
            Assert.Equal("ISample", _converter.ConvertType("ISample"));
            Assert.Equal("CSample", _converter.ConvertType("CSample"));
        }
    }
}