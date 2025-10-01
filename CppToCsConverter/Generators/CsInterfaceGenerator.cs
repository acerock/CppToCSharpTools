using System;
using System.Linq;
using System.Text;
using CppToCsConverter.Models;

namespace CppToCsConverter.Generators
{
    public class CsInterfaceGenerator
    {
        private readonly TypeConverter _typeConverter;

        public CsInterfaceGenerator()
        {
            _typeConverter = new TypeConverter();
        }

        public string GenerateInterface(CppClass cppInterface)
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
                    var methodSignature = GenerateExtensionMethodSignature(cppInterface.Name, staticMethod);
                    sb.AppendLine($"        {methodSignature}");
                    sb.AppendLine("        {");
                    sb.AppendLine("            // TODO: Implement extension method");
                    sb.AppendLine("            throw new NotImplementedException();");
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
            var returnType = _typeConverter.ConvertType(method.ReturnType);
            var parameters = string.Join(", ", method.Parameters.Select(GenerateParameter));

            return $"{returnType} {method.Name}({parameters})";
        }

        private string GenerateExtensionMethodSignature(string interfaceName, CppMethod method)
        {
            var returnType = _typeConverter.ConvertType(method.ReturnType);
            var parameters = $"this {interfaceName} instance";
            
            if (method.Parameters.Any())
            {
                var methodParams = string.Join(", ", method.Parameters.Select(GenerateParameter));
                parameters += ", " + methodParams;
            }

            return $"public static {returnType} {method.Name}({parameters})";
        }

        private string GenerateParameter(CppParameter param)
        {
            var csType = _typeConverter.ConvertType(param.Type);
            var modifier = "";

            // Handle pointer parameters as out/ref
            if (param.IsPointer && !param.IsConst)
            {
                modifier = "out ";
            }
            else if (param.IsReference && !param.IsConst)
            {
                modifier = "ref ";
            }

            return $"{modifier}{csType} {param.Name}";
        }
    }
}