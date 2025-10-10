using System.Collections.Generic;

namespace CppToCsConverter.Core.Models
{
    public class CppDefine
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string FullDefinition { get; set; } = string.Empty; // "#define NAME value"
        public List<string> PrecedingComments { get; set; } = new List<string>();
        public string SourceFileName { get; set; } = string.Empty; // Track which file it came from
    }
}