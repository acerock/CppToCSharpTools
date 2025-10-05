namespace CppToCsConverter.Core.Models
{
    public class CppStaticMemberInit
    {
        public string ClassName { get; set; } = string.Empty;
        public string MemberName { get; set; } = string.Empty;
        public string InitializationValue { get; set; } = string.Empty;
        public bool IsArray { get; set; }
        public string ArraySize { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool IsConst { get; set; }
    }
}