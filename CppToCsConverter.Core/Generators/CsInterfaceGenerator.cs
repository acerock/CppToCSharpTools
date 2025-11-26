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

            // Add file-scoped namespace
            sb.AppendLine("namespace GeneratedInterfaces;");
            sb.AppendLine();

            // Add Create attribute for public interfaces with resolved implementing class
            var accessibility = cppInterface.IsPublicExport ? "public" : "internal";
            if (cppInterface.IsPublicExport && sourceImplementations != null)
            {
                var implementingClass = ResolveImplementingClassFromFactory(cppInterface, sourceImplementations);
                if (!string.IsNullOrEmpty(implementingClass))
                {
                    sb.AppendLine($"[Create(typeof({implementingClass}))]");
                }
            }

            // Interface declaration
            sb.AppendLine($"{accessibility} interface {cppInterface.Name}");
            sb.AppendLine("{");

            // Add methods (skip constructors, destructors, and static methods for interfaces)
            var interfaceMethods = cppInterface.Methods
                .Where(m => !m.IsConstructor && !m.IsDestructor && !m.IsStatic)
                .Where(m => m.AccessSpecifier == AccessSpecifier.Public);

            foreach (var method in interfaceMethods)
            {
                var methodSignature = GenerateMethodSignature(method);
                sb.AppendLine($"    {methodSignature};");
                sb.AppendLine();
            }

            sb.AppendLine("}");

            return sb.ToString();
        }

        private string? ResolveImplementingClassFromFactory(CppClass cppInterface, List<CppMethod> sourceImplementations)
        {
            // Look for static factory method (e.g., GetInstance, CreateInstance, etc.)
            var staticFactoryMethod = cppInterface.Methods
                .FirstOrDefault(m => m.IsStatic && m.AccessSpecifier == AccessSpecifier.Public);

            if (staticFactoryMethod == null)
                return null;

            // Find implementation in source files
            var implementation = sourceImplementations.FirstOrDefault(impl =>
                impl.ClassName == cppInterface.Name &&
                impl.Name == staticFactoryMethod.Name &&
                !string.IsNullOrEmpty(impl.ImplementationBody));

            if (implementation == null || string.IsNullOrEmpty(implementation.ImplementationBody))
                return null;

            // Parse the implementation body to find the implementing class
            // Look for patterns like: CSample* pSample = new CSample();
            // or: return new CSample();
            var body = implementation.ImplementationBody;
            
            // Pattern 1: new ClassName()
            var newPattern = new System.Text.RegularExpressions.Regex(@"new\s+([A-Z][A-Za-z0-9_]*)\s*\(");
            var match = newPattern.Match(body);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            // Pattern 2: ClassName* variable = new ClassName()
            var declarationPattern = new System.Text.RegularExpressions.Regex(@"([A-Z][A-Za-z0-9_]*)\s*\*\s*\w+\s*=\s*new\s+([A-Z][A-Za-z0-9_]*)\s*\(");
            match = declarationPattern.Match(body);
            if (match.Success)
            {
                return match.Groups[2].Value;
            }

            return null;
        }

        private string GenerateMethodSignature(CppMethod method)
        {
            var returnType = method.ReturnType; // Preserve original C++ return type
            var parameters = string.Join(", ", method.Parameters.Select(GenerateParameter));

            return $"{returnType} {method.Name}({parameters})";
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
    }
}