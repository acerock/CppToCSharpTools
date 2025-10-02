using System.Collections.Generic;

namespace CppToCsConverter.Models
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
        
        public AccessSpecifier DefaultAccessSpecifier => IsInterface ? AccessSpecifier.Public : AccessSpecifier.Private;
    }

    public class CppMember
    {
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public AccessSpecifier AccessSpecifier { get; set; }
        public bool IsStatic { get; set; }
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
    }

    public class CppMemberInitializer
    {
        public string MemberName { get; set; } = string.Empty;
        public string InitializationValue { get; set; } = string.Empty;
    }

    public enum AccessSpecifier
    {
        Private,
        Protected,
        Public
    }
}