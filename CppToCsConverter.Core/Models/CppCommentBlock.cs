using System.Collections.Generic;

namespace CppToCsConverter.Core.Models
{
    public enum CommentType
    {
        SingleLine,   // //
        MultiLine     // /* */
    }

    public class CppCommentBlock
    {
        public List<string> Lines { get; set; } = new List<string>();
        public CommentType Type { get; set; }
    }
}