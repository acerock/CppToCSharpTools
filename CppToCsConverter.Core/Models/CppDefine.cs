using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CppToCsConverter.Core.Models
{
    public class CppDefine
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string FullDefinition { get; set; } = string.Empty; // "#define NAME value"
        public List<string> PrecedingComments { get; set; } = new List<string>();
        public string SourceFileName { get; set; } = string.Empty; // Track which file it came from
        public bool IsFromHeader { get; set; } = false; // True if from .h file, false if from .cpp file
        
        /// <summary>
        /// Infers the C# type from the define value.
        /// Returns one of: char, string, int, long, double, bool
        /// </summary>
        public string InferType()
        {
            if (string.IsNullOrWhiteSpace(Value))
                return "int"; // Default to int for empty values
            
            var trimmedValue = Value.Trim();
            
            // Check for character literals: _T('x'), 'x', _T('\t')
            if (Regex.IsMatch(trimmedValue, @"^_T\s*\(\s*'.*?'\s*\)$") || 
                Regex.IsMatch(trimmedValue, @"^'.*?'$"))
            {
                return "char";
            }
            
            // Check for string literals: _T("..."), "...", _("")
            if (Regex.IsMatch(trimmedValue, @"^_T\s*\(\s*"".*?""\s*\)$") || 
                Regex.IsMatch(trimmedValue, @"^_\s*\(\s*"".*?""\s*\)$") ||
                Regex.IsMatch(trimmedValue, @"^"".*?""$"))
            {
                return "string";
            }
            
            // Check for boolean values: TRUE, FALSE, YES, NO, OK, NOTOK, true, false
            var upperValue = trimmedValue.ToUpperInvariant();
            if (upperValue == "TRUE" || upperValue == "YES" || upperValue == "OK" ||
                upperValue == "FALSE" || upperValue == "NO" || upperValue == "NOTOK" ||
                upperValue == "true" || upperValue == "false")
            {
                return "bool";
            }
            
            // Check for long: ends with 'L' or 'l'
            if (Regex.IsMatch(trimmedValue, @"^-?\d+[Ll]$"))
            {
                return "long";
            }
            
            // Check for double: contains decimal point or ends with 'D'/'d'
            if (Regex.IsMatch(trimmedValue, @"^-?\d+\.\d+$") || 
                Regex.IsMatch(trimmedValue, @"^-?\d+[Dd]$"))
            {
                return "double";
            }
            
            // Check for integer: plain numeric value
            if (Regex.IsMatch(trimmedValue, @"^-?\d+$"))
            {
                return "int";
            }
            
            // Default to int for unrecognized patterns
            return "int";
        }
        
        /// <summary>
        /// Normalizes the C++ value to C# equivalent.
        /// Converts _T(""), TRUE/FALSE, etc. to their C# forms.
        /// </summary>
        public string NormalizeValue()
        {
            if (string.IsNullOrWhiteSpace(Value))
                return "0"; // Default for empty values
            
            var trimmedValue = Value.Trim();
            
            // Handle character literals: _T('x') -> 'x'
            var charMatch = Regex.Match(trimmedValue, @"^_T\s*\(\s*('.*?')\s*\)$");
            if (charMatch.Success)
            {
                return charMatch.Groups[1].Value;
            }
            
            // Handle string literals: _T("...") -> "...", _("...") -> "..."
            var stringMatch = Regex.Match(trimmedValue, @"^_T?\s*\(\s*("".*?"")\s*\)$");
            if (stringMatch.Success)
            {
                return stringMatch.Groups[1].Value;
            }
            
            // Handle boolean literals: TRUE -> true, FALSE -> false
            var upperValue = trimmedValue.ToUpperInvariant();
            if (upperValue == "TRUE" || upperValue == "YES" || upperValue == "OK")
                return "true";
            if (upperValue == "FALSE" || upperValue == "NO" || upperValue == "NOTOK")
                return "false";
            
            // For numbers, long suffixes, etc., return as-is
            // The value is already in a form C# can understand
            return trimmedValue;
        }
        
        /// <summary>
        /// Gets the access modifier for this define.
        /// Header defines -> "internal", Source defines -> "private"
        /// </summary>
        public string GetAccessModifier()
        {
            return IsFromHeader ? "internal" : "private";
        }
        
        /// <summary>
        /// Generates the C# const declaration from this define.
        /// Example: "internal const int WARNING = 1;"
        /// </summary>
        public string ToCSharpConst()
        {
            var accessModifier = GetAccessModifier();
            var type = InferType();
            var normalizedValue = NormalizeValue();
            
            return $"{accessModifier} const {type} {Name} = {normalizedValue};";
        }
    }
}