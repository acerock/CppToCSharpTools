using System.Collections.Generic;

namespace CppToCsConverter.Core.Models
{
    public enum StructType
    {
        Simple,        // struct MyStruct { ... };
        Typedef,       // typedef struct { ... } MyStruct;
        TypedefTag     // typedef struct MyTag { ... } MyStruct;
    }

    /// <summary>
    /// Represents a C++ struct that should be copied as-is to C# without transformation
    /// </summary>
    public class CppStruct
    {
        public string Name { get; set; } = string.Empty;
        public string OriginalDefinition { get; set; } = string.Empty; // Complete struct definition as-is from C++
        public StructType Type { get; set; }
        public List<string> PrecedingComments { get; set; } = new List<string>(); // Comments before struct declaration
    }
}