using System.Collections.Generic;

namespace CppToCsConverter.Core.Models
{
    public class CppMember
    {
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public AccessSpecifier AccessSpecifier { get; set; }
        public bool IsStatic { get; set; }
        public bool IsConst { get; set; }
        public string InitializationValue { get; set; } = string.Empty; // For const members with initialization (e.g., const int x = 5)
        public bool IsArray { get; set; }
        public string ArraySize { get; set; } = string.Empty;
        public List<string> PrecedingComments { get; set; } = new List<string>(); // Comments before member declaration
        public string PostfixComment { get; set; } = string.Empty; // Comment on the same line as member declaration (after the semicolon)
        public string RegionStart { get; set; } = string.Empty; // Region start marker (from .h file - converted to comment)
        public string RegionEnd { get; set; } = string.Empty; // Region end marker (from .h file - converted to comment)
        public int OrderIndex { get; set; } // Position in file relative to other elements
    }
}