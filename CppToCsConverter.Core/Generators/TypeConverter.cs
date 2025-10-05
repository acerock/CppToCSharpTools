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
                // All C++ types preserved for downstream processing
                // Only keeping identity mappings for modifier cleanup purposes
                { "void", "void" },
                { "bool", "bool" },
                { "char", "char" },
                { "short", "short" },
                { "int", "int" },
                { "long", "long" },
                { "float", "float" },
                { "double", "double" },
                // Note: unsigned char, unsigned int, size_t etc. preserved as-is
                
                // Common C++ types - preserve for downstream processing
                // Note: agrint, CString etc. are preserved as-is
                
                // Windows API types - preserve for downstream processing
                // Note: DWORD, LPSTR etc. are preserved as-is
                
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

            // Preserve C++ default values as-is for downstream processing
            // The downstream tools will handle conversion to appropriate C# defaults
            return cppDefaultValue.Trim();
        }

        private string ConvertTemplateType(string templateType)
        {
            // Preserve std:: types as-is since downstream tools will handle translation
            if (templateType.StartsWith("std::"))
            {
                return templateType;
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