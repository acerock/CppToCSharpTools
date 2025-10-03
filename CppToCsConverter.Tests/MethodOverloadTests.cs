using System;
using System.Reflection;
using Xunit;
using CppToCsConverter.Core;

namespace CppToCsConverter.Tests
{
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
            var sig1 = InvokeGetMethodSignature(converter, method1);
            var sig2 = InvokeGetMethodSignature(converter, method2);
            var sig3 = InvokeGetMethodSignature(converter, method3);
            
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
            Assert.Equal("tdimvalue", InvokeNormalizeParameterType(converter, "const TDimValue&"));
            Assert.Equal("tdimvalue", InvokeNormalizeParameterType(converter, "TDimValue"));
            Assert.Equal("tdimvalue", InvokeNormalizeParameterType(converter, "const TDimValue *"));
            Assert.Equal("agrint", InvokeNormalizeParameterType(converter, "const agrint&"));
            Assert.Equal("agrint", InvokeNormalizeParameterType(converter, "agrint"));
        }
        
        // Helper methods
        private static CppToCsConverter.Models.CppMethod CreateTestMethod(string name, string[] paramTypes)
        {
            var method = new CppToCsConverter.Models.CppMethod { Name = name };
            foreach (var paramType in paramTypes)
            {
                method.Parameters.Add(new CppToCsConverter.Models.CppParameter 
                { 
                    Type = paramType, 
                    Name = $"param{method.Parameters.Count + 1}" 
                });
            }
            return method;
        }
        
        private static string InvokeGetMethodSignature(CppToCsStructuralConverter converter, CppToCsConverter.Models.CppMethod method)
        {
            var methodInfo = typeof(CppToCsStructuralConverter).GetMethod("GetMethodSignature", BindingFlags.NonPublic | BindingFlags.Instance);
            return (string)methodInfo.Invoke(converter, new object[] { method });
        }
        
        private static string InvokeNormalizeParameterType(CppToCsStructuralConverter converter, string type)
        {
            var methodInfo = typeof(CppToCsStructuralConverter).GetMethod("NormalizeParameterType", BindingFlags.NonPublic | BindingFlags.Instance);
            return (string)methodInfo.Invoke(converter, new object[] { type });
        }
    }
}