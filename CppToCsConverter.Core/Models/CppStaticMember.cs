namespace CppToCsConverter.Core.Models
{
    public class CppStaticMember
    {
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string InitializationValue { get; set; } = string.Empty;
        public bool IsArray { get; set; }
        public string ArraySize { get; set; } = string.Empty;
    }
}