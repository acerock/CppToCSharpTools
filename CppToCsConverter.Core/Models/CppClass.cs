using System.Collections.Generic;
using System.Linq;

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
        public List<CppDefine> HeaderDefines { get; set; } = new List<CppDefine>(); // Define statements from header file
        public List<CppDefine> SourceDefines { get; set; } = new List<CppDefine>(); // Define statements from source files
        
        public AccessSpecifier DefaultAccessSpecifier => IsInterface ? AccessSpecifier.Public : AccessSpecifier.Private;
        
        /// <summary>
        /// Determines if this class should be generated as partial classes based on TargetFileName distribution
        /// </summary>
        public bool IsPartialClass()
        {
            if (!Methods.Any()) return false;
            
            // Get all unique target file names from methods that have implementations or inline code
            var targetFileNames = Methods
                .Where(m => !string.IsNullOrEmpty(m.TargetFileName))
                .Select(m => m.TargetFileName)
                .Distinct()
                .ToList();
            
            // A class is partial ONLY if methods are distributed across multiple target files
            return targetFileNames.Count > 1;
        }
        
        /// <summary>
        /// Gets all unique target file names where this class has method implementations
        /// </summary>
        public List<string> GetTargetFileNames()
        {
            return Methods
                .Where(m => !string.IsNullOrEmpty(m.TargetFileName))
                .Select(m => m.TargetFileName)
                .Distinct()
                .ToList();
        }
        
        /// <summary>
        /// Gets methods grouped by their target file names for partial class generation
        /// </summary>
        public Dictionary<string, List<CppMethod>> GetMethodsByTargetFile()
        {
            return Methods
                .Where(m => !string.IsNullOrEmpty(m.TargetFileName))
                .GroupBy(m => m.TargetFileName)
                .ToDictionary(g => g.Key, g => g.OrderBy(m => m.OrderIndex).ToList());
        }
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
        public string PostfixComment { get; set; } = string.Empty; // Comment on the same line as member declaration (after the semicolon)
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

    public enum CommentPosition
    {
        Prefix,  // Comment appears before the parameter type/name
        Suffix   // Comment appears after the parameter type/name
    }

    public class ParameterComment
    {
        public string CommentText { get; set; } = string.Empty;
        public CommentPosition Position { get; set; }
    }

    public class CppParameter
    {
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DefaultValue { get; set; } = string.Empty;
        public bool IsReference { get; set; }
        public bool IsPointer { get; set; }
        public bool IsConst { get; set; }
        public List<string> InlineComments { get; set; } = new List<string>(); // Comments within the parameter list (legacy)
        public List<ParameterComment> PositionedComments { get; set; } = new List<ParameterComment>(); // Comments with position information
        public string OriginalText { get; set; } = string.Empty; // Original parameter text with comments for reconstruction
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

    public class CppDefine
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string FullDefinition { get; set; } = string.Empty; // "#define NAME value"
        public List<string> PrecedingComments { get; set; } = new List<string>();
        public string SourceFileName { get; set; } = string.Empty; // Track which file it came from
    }

    public enum AccessSpecifier
    {
        Private,
        Protected,
        Public
    }
}