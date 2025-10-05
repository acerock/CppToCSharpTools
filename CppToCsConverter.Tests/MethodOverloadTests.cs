using System;
using Xunit;
using CppToCsConverter.Core.Core;
using CppToCsConverter.Core.Models;

namespace CppToCsConverter.Tests
{
    /// <summary>
    /// Tests for method overload handling and signature generation.
    /// Uses friend assembly access to test internal methods directly instead of reflection.
    /// </summary>
    public class MethodOverloadTests
    {
        [Fact]
        public void GetMethodSignature_WithDifferentParameterTypes_ShouldCreateUniqueSignatures()
        {
            // Arrange
            var converter = new CppToCsStructuralConverter();
            var method1 = CreateTestMethod("TestMethod", new[] { "int" });
            var method2 = CreateTestMethod("TestMethod", new[] { "int", "string" });
            var method3 = CreateTestMethod("TestMethod", new[] { "double" });
            
            // Act
            var sig1 = converter.GetMethodSignature(method1);
            var sig2 = converter.GetMethodSignature(method2);
            var sig3 = converter.GetMethodSignature(method3);
            
            // Assert
            Assert.NotEqual(sig1, sig2);
            Assert.NotEqual(sig1, sig3);
            Assert.NotEqual(sig2, sig3);
            
            Assert.Contains("TestMethod", sig1);
            Assert.Contains("TestMethod", sig2);
            Assert.Contains("TestMethod", sig3);
        }
        
        [Fact] 
        public void NormalizeParameterType_WithConstAndReference_ShouldNormalizeCorrectly()
        {
            // Arrange
            var converter = new CppToCsStructuralConverter();
            
            // Act & Assert
            Assert.Equal("tdimvalue", converter.NormalizeParameterType("const TDimValue&"));
            Assert.Equal("tdimvalue", converter.NormalizeParameterType("TDimValue"));
            Assert.Equal("tdimvalue", converter.NormalizeParameterType("const TDimValue *"));
            Assert.Equal("agrint", converter.NormalizeParameterType("const agrint&"));
            Assert.Equal("agrint", converter.NormalizeParameterType("agrint"));
        }
        
        // Helper methods
        private static CppMethod CreateTestMethod(string name, string[] paramTypes)
        {
            var method = new CppMethod { Name = name };
            foreach (var paramType in paramTypes)
            {
                method.Parameters.Add(new CppParameter 
                { 
                    Type = paramType, 
                    Name = $"param{method.Parameters.Count + 1}" 
                });
            }
            return method;
        }

    }
}