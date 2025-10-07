using System.Collections.Generic;

namespace CppToCsConverter.Core.Models
{
    public class CppClass
    {
        public string Name { get; set; } = string.Empty;
        public bool IsInterface { get; set; }
        public bool IsPublicExport { get; set; }
        public List<string> BaseClasses { get; set; } = new List<string>();
        public List<CppMember> Members { get; set; } = new List<CppMember>();
        public List<CppMethod> Methods { get; set; } = new List<CppMethod>();
        public List<CppStaticMember> StaticMembers { get; set; } = new List<CppStaticMember>();
        public List<string> PrecedingComments { get; set; } = new List<string>(); // Comments before class declaration
        
        public AccessSpecifier DefaultAccessSpecifier => IsInterface ? AccessSpecifier.Public : AccessSpecifier.Private;
    }

    public class CppMember
    {
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public AccessSpecifier AccessSpecifier { get; set; }
        public bool IsStatic { get; set; }
        public bool IsArray { get; set; }
        public string ArraySize { get; set; } = string.Empty;
        public List<string> PrecedingComments { get; set; } = new List<string>(); // Comments before member declaration
        public string RegionStart { get; set; } = string.Empty; // Region start marker (from .h file - converted to comment)
        public string RegionEnd { get; set; } = string.Empty; // Region end marker (from .h file - converted to comment)
    }

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
        public bool HasInlineImplementation { get; set; }
        public string InlineImplementation { get; set; } = string.Empty;
        public string ImplementationBody { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty; // For source file parsing
        public int OrderIndex { get; set; } // For maintaining order from .cpp files
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

    public class CppParameter
    {
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DefaultValue { get; set; } = string.Empty;
        public bool IsReference { get; set; }
        public bool IsPointer { get; set; }
        public bool IsConst { get; set; }
    }

    public class CppStaticMember
    {
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string InitializationValue { get; set; } = string.Empty;
        public bool IsArray { get; set; }
        public string ArraySize { get; set; } = string.Empty;
    }

    public class CppMemberInitializer
    {
        public string MemberName { get; set; } = string.Empty;
        public string InitializationValue { get; set; } = string.Empty;
    }

    public class CppCommentBlock
    {
        public List<string> Lines { get; set; } = new List<string>();
        public CommentType Type { get; set; }
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

    public enum StructType
    {
        Simple,        // struct MyStruct { ... };
        Typedef,       // typedef struct { ... } MyStruct;
        TypedefTag     // typedef struct MyTag { ... } MyStruct;
    }

    public enum CommentType
    {
        SingleLine,   // //
        MultiLine     // /* */
    }

    public enum AccessSpecifier
    {
        Private,
        Protected,
        Public
    }
}