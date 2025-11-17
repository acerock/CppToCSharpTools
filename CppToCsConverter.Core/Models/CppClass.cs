using System.Collections.Generic;
using System.Linq;

namespace CppToCsConverter.Core.Models
{
    public class CppClass
    {
        public string Name { get; set; } = string.Empty;
        public bool IsInterface { get; set; }
        public bool IsStruct { get; set; } // True if this was a C++ struct (becomes internal class in C#)
        public bool IsPublicExport { get; set; }
        public List<string> BaseClasses { get; set; } = new List<string>();
        public List<CppMember> Members { get; set; } = new List<CppMember>();
        public List<CppMethod> Methods { get; set; } = new List<CppMethod>();
        public List<CppStaticMember> StaticMembers { get; set; } = new List<CppStaticMember>();
        public List<string> PrecedingComments { get; set; } = new List<string>(); // Comments before class declaration
        public List<CppDefine> HeaderDefines { get; set; } = new List<CppDefine>(); // Define statements from header file
        public List<CppDefine> SourceDefines { get; set; } = new List<CppDefine>(); // Define statements from source files
        
        public AccessSpecifier DefaultAccessSpecifier => IsInterface ? AccessSpecifier.Public : 
                                                          IsStruct ? AccessSpecifier.Internal : 
                                                          AccessSpecifier.Private;
        
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
}