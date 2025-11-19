using System.Collections.Generic;

namespace CppToCsConverter.Core.Models
{
    /// <summary>
    /// Represents a C++ source file (.cpp) with all its parsed content including file-level metadata
    /// </summary>
    public class CppSourceFile
    {
        public string FileName { get; set; } = string.Empty;
        public List<string> FileTopComments { get; set; } = new List<string>(); // Comments before #include statements
        public List<CppMethod> Methods { get; set; } = new List<CppMethod>();
        public List<CppStaticMemberInit> StaticMemberInits { get; set; } = new List<CppStaticMemberInit>();
        public List<CppDefine> Defines { get; set; } = new List<CppDefine>();
        public List<CppStruct> Structs { get; set; } = new List<CppStruct>(); // Structs/classes defined in source file
    }
}