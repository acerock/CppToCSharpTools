using System.Collections.Generic;

namespace CppToCsConverter.Core.Models
{
    public class CppMethod
    {
        public string Name { get; set; } = string.Empty;
        public string ReturnType { get; set; } = string.Empty;
        public List<CppParameter> Parameters { get; set; } = new List<CppParameter>();
        public AccessSpecifier AccessSpecifier { get; set; }
        public bool IsStatic { get; set; }
        public bool IsVirtual { get; set; }
        public bool IsPureVirtual { get; set; }
        public bool IsConstructor { get; set; }
        public bool IsDestructor { get; set; }
        public bool IsConst { get; set; }
        public bool IsLocalMethod { get; set; } = false; // Local method without class scope regulator (::)
        public bool HasInlineImplementation { get; set; }
        public string InlineImplementation { get; set; } = string.Empty;
        public string ImplementationBody { get; set; } = string.Empty;
        public bool HasResolvedImplementation { get; set; } = false; // True if implementation was found in source file (even if body is empty)
        public string ClassName { get; set; } = string.Empty; // For source file parsing
        public int OrderIndex { get; set; } // For maintaining order from .cpp files
        public string TargetFileName { get; set; } = string.Empty; // Target .cs file name (without extension) - .cpp file name for implementations, .h file name for inline methods
        public List<CppMemberInitializer> MemberInitializerList { get; set; } = new List<CppMemberInitializer>();
        public List<string> HeaderComments { get; set; } = new List<string>(); // Comments from .h file
        public List<string> SourceComments { get; set; } = new List<string>(); // Comments from .cpp file
        public string HeaderRegionStart { get; set; } = string.Empty; // Region start from .h file (converted to comment)
        public string HeaderRegionEnd { get; set; } = string.Empty; // Region end from .h file (converted to comment)  
        public string SourceRegionStart { get; set; } = string.Empty; // Region start from .cpp file (preserved as region)
        public string SourceRegionEnd { get; set; } = string.Empty; // Region end from .cpp file (preserved as region)
        
        // Indentation context properties for proper alignment
        public int HeaderCommentIndentation { get; set; } = 0; // Original indentation level of header comments
        public int SourceCommentIndentation { get; set; } = 0; // Original indentation level of source comments  
        public int ImplementationIndentation { get; set; } = 0; // Original indentation level of method body
    }
}