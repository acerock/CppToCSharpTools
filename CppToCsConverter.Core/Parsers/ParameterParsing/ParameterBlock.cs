namespace CppToCsConverter.Core.Parsers.ParameterParsing;

/// <summary>
/// Represents a raw parameter block - the text between commas in a parameter list.
/// This is an intermediate representation before parsing into structured components.
/// </summary>
public class ParameterBlock
{
    /// <summary>
    /// The raw text of this parameter block, including all whitespace and comments.
    /// </summary>
    public string RawText { get; }

    /// <summary>
    /// Zero-based index of this parameter in the original parameter list.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Whether this parameter block starts on a new line (has leading newline).
    /// </summary>
    public bool StartsOnNewLine { get; }

    /// <summary>
    /// Number of leading spaces/tabs before the first non-whitespace character.
    /// Only meaningful if StartsOnNewLine is true.
    /// </summary>
    public int LeadingIndent { get; }

    public ParameterBlock(string rawText, int index, bool startsOnNewLine, int leadingIndent)
    {
        RawText = rawText ?? throw new ArgumentNullException(nameof(rawText));
        Index = index;
        StartsOnNewLine = startsOnNewLine;
        LeadingIndent = leadingIndent;
    }

    public override string ToString() => $"Block[{Index}]: {RawText.Trim()}";
}
