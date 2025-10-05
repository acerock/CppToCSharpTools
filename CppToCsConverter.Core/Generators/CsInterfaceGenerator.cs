using System;
using System.Linq;
using System.Text;
using CppToCsConverter.Core.Models;

namespace CppToCsConverter.Core.Generators
{
    public class CsInterfaceGenerator
    {
        private readonly TypeConverter _typeConverter;

        public CsInterfaceGenerator()
        {
            _typeConverter = new TypeConverter();
        }

        public string GenerateInterface(CppClass cppInterface, List<CppMethod>? sourceImplementations = null)
        {
            var sb = new StringBuilder();
            
            // Add using statements
            sb.AppendLine("using System;");
            sb.AppendLine();

            // Add namespace (you might want to make this configurable)
            sb.AppendLine("namespace GeneratedInterfaces");
            sb.AppendLine("{");

            // Interface declaration
            var accessibility = cppInterface.IsPublicExport ? "public" : "internal";
            sb.AppendLine($"    {accessibility} interface {cppInterface.Name}");
            sb.AppendLine("    {");

            // Add methods (skip constructors, destructors, and static methods for interfaces)
            var interfaceMethods = cppInterface.Methods
                .Where(m => !m.IsConstructor && !m.IsDestructor && !m.IsStatic)
                .Where(m => m.AccessSpecifier == AccessSpecifier.Public);

            foreach (var method in interfaceMethods)
            {
                var methodSignature = GenerateMethodSignature(method);
                sb.AppendLine($"        {methodSignature};");
                sb.AppendLine();
            }

            sb.AppendLine("    }");

            // Generate extension class for static methods if any exist
            var staticMethods = cppInterface.Methods
                .Where(m => m.IsStatic && m.AccessSpecifier == AccessSpecifier.Public)
                .ToList();

            if (staticMethods.Any())
            {
                sb.AppendLine();
                sb.AppendLine($"    public static class {cppInterface.Name}Extensions");
                sb.AppendLine("    {");

                foreach (var staticMethod in staticMethods)
                {
                    // Find implementation in source files
                    var implementation = sourceImplementations?.FirstOrDefault(impl => 
                        impl.ClassName == cppInterface.Name && 
                        impl.Name == staticMethod.Name && 
                        !string.IsNullOrEmpty(impl.ImplementationBody));
                    
                    var methodSignature = GenerateExtensionMethodSignature(cppInterface.Name, staticMethod);
                    sb.AppendLine($"        {methodSignature}");
                    sb.AppendLine("        {");
                    
                    if (implementation != null && !string.IsNullOrEmpty(implementation.ImplementationBody))
                    {
                        // Use actual implementation body
                        var indentedBody = IndentMethodBody(implementation.ImplementationBody, 3);
                        sb.Append(indentedBody);
                    }
                    else
                    {
                        sb.AppendLine("            // TODO: Implementation not found");
                        sb.AppendLine("            throw new NotImplementedException();");
                    }
                    
                    sb.AppendLine("        }");
                    sb.AppendLine();
                }

                sb.AppendLine("    }");
            }

            sb.AppendLine("}");

            return sb.ToString();
        }

        private string GenerateMethodSignature(CppMethod method)
        {
            var returnType = method.ReturnType; // Preserve original C++ return type
            var parameters = string.Join(", ", method.Parameters.Select(GenerateParameter));

            return $"{returnType} {method.Name}({parameters})";
        }

        private string GenerateExtensionMethodSignature(string interfaceName, CppMethod method)
        {
            var returnType = ConvertTypeForExtensionMethod(method.ReturnType);
            var parameters = $"this {interfaceName} instance";
            
            if (method.Parameters.Any())
            {
                var methodParams = string.Join(", ", method.Parameters.Select(GenerateParameter));
                parameters += ", " + methodParams;
            }

            return $"public static {returnType} {method.Name}({parameters})";
        }

        private string ConvertTypeForExtensionMethod(string cppType)
        {
            // For extension methods, convert C++ pointer types to C# reference types
            // Remove trailing pointer indicator if present
            if (cppType.EndsWith("*"))
            {
                return cppType.Substring(0, cppType.Length - 1).Trim();
            }
            
            // Return the type as-is if no conversion needed
            return cppType;
        }

        private string GenerateParameter(CppParameter param)
        {
            // Preserve original C++ parameter syntax exactly as-is
            var result = "";
            
            if (param.IsConst)
                result += "const ";
                
            result += param.Type;
            
            if (param.IsReference)
                result += "&";
            else if (param.IsPointer)
                result += "*";
                
            result += " " + param.Name;
            
            if (!string.IsNullOrEmpty(param.DefaultValue))
                result += " = " + param.DefaultValue;
                
            return result;
        }
        
        private string IndentMethodBody(string methodBody, int indentLevel)
        {
            if (string.IsNullOrEmpty(methodBody))
                return string.Empty;
                
            var lines = methodBody.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
            var sb = new StringBuilder();
            var indent = new string(' ', indentLevel * 4);
            
            foreach (var line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    sb.AppendLine(indent + line.TrimStart());
                }
                else
                {
                    sb.AppendLine();
                }
            }
            
            return sb.ToString();
        }
    }
}