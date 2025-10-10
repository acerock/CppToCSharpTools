using System.Collections.Generic;

namespace CppToCsConverter.Core.Models
{
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
}