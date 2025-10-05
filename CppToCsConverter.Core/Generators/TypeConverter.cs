using System;
using System.Collections.Generic;

namespace CppToCsConverter.Core.Generators
{
    public class TypeConverter
    {
        private readonly Dictionary<string, string> _typeMap;

        public TypeConverter()
        {
            _typeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // Basic types
                { "void", "void" },
                { "bool", "bool" },
                { "char", "char" },
                { "unsigned char", "byte" },
                { "short", "short" },
                { "unsigned short", "ushort" },
                { "int", "int" },
                { "unsigned int", "uint" },
                { "long", "long" },
                { "unsigned long", "ulong" },
                { "float", "float" },
                { "double", "double" },
                { "size_t", "ulong" },
                
                // Common C++ types
                { "agrint", "int" }, // Based on the sample code
                
                // MFC types (assuming these are available)
                { "CString", "string" }, // or keep as CString if you have MFC wrapper
                { "DWORD", "uint" },
                { "WORD", "ushort" },
                { "LPSTR", "string" },
                { "LPCSTR", "string" },
                { "LPWSTR", "string" },
                { "LPCWSTR", "string" },
                
                // Custom types (these might need to be preserved or mapped)
                { "TDimValue", "TDimValue" }, // Assuming this struct exists in C#
                { "TAttId", "TAttId" }, // Assuming this struct exists in C#
            };
        }

        public string ConvertType(string cppType)
        {
            if (string.IsNullOrWhiteSpace(cppType))
                return "void";

            // Remove whitespace and normalize
            var normalizedType = cppType.Trim();
            
            // Handle const keyword
            if (normalizedType.StartsWith("const "))
            {
                normalizedType = normalizedType.Substring(6).Trim();
            }

            // Handle pointers and references - remove them for basic conversion
            // In C#, these will be handled as ref/out parameters or nullable types
            normalizedType = normalizedType.TrimEnd('*', '&').Trim();

            // Check direct mapping
            if (_typeMap.TryGetValue(normalizedType, out var csType))
            {
                return csType;
            }

            // Handle template types (basic conversion)
            if (normalizedType.Contains("<"))
            {
                return ConvertTemplateType(normalizedType);
            }

            // If no mapping found, assume it's a custom type that should be preserved
            return normalizedType;
        }

        public string ConvertDefaultValue(string cppDefaultValue)
        {
            if (string.IsNullOrWhiteSpace(cppDefaultValue))
                return "";

            var normalized = cppDefaultValue.Trim();

            // Handle common C++ default values
            switch (normalized.ToLower())
            {
                case "null":
                case "nullptr":
                case "0":
                    return "null";
                case "true":
                case "false":
                    return normalized.ToLower();
                case "\"\"":
                    return "string.Empty";
            }

            // Handle _T macro
            if (normalized.StartsWith("_T(\"") && normalized.EndsWith("\")"))
            {
                return normalized.Substring(3, normalized.Length - 5) + "\"";
            }

            if (normalized.StartsWith("_T(") && normalized.EndsWith(")"))
            {
                return normalized.Substring(3, normalized.Length - 4);
            }

            // Handle numeric values
            if (int.TryParse(normalized, out _) || 
                double.TryParse(normalized, out _) ||
                float.TryParse(normalized, out _))
            {
                return normalized;
            }

            // For anything else, return as-is with a comment
            return $"{normalized} /* TODO: Verify default value */";
        }

        private string ConvertTemplateType(string templateType)
        {
            // Basic template conversion - this could be expanded
            // For now, just preserve the structure
            
            // Example: std::vector<int> -> List<int>
            if (templateType.StartsWith("std::vector<"))
            {
                var innerType = ExtractTemplateParameter(templateType);
                var convertedInnerType = ConvertType(innerType);
                return $"List<{convertedInnerType}>";
            }

            // Example: std::string -> string
            if (templateType == "std::string")
            {
                return "string";
            }

            // For other templates, preserve as-is
            return templateType;
        }

        private string ExtractTemplateParameter(string templateType)
        {
            var startIndex = templateType.IndexOf('<');
            var endIndex = templateType.LastIndexOf('>');
            
            if (startIndex >= 0 && endIndex > startIndex)
            {
                return templateType.Substring(startIndex + 1, endIndex - startIndex - 1).Trim();
            }

            return "object"; // fallback
        }

        public void AddCustomTypeMapping(string cppType, string csType)
        {
            _typeMap[cppType] = csType;
        }
    }
}