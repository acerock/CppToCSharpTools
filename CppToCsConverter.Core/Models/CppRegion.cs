namespace CppToCsConverter.Core.Models
{
    /// <summary>
    /// Represents a #region or #endregion marker in C++ source code
    /// These are treated as ordered elements alongside methods and members
    /// </summary>
    public class CppRegion
    {
        public string Text { get; set; } = string.Empty; // "#region Name" or "#endregion"
        public bool IsStart { get; set; } // true for #region, false for #endregion
        public int OrderIndex { get; set; } // Position in file relative to other elements
        public string SourceFileName { get; set; } = string.Empty;
        public bool IsFromHeader { get; set; } = false; // true if from .h, false if from .cpp
    }
}
