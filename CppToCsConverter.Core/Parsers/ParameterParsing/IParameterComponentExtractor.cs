using CppToCsConverter.Core.Models;

namespace CppToCsConverter.Core.Parsers.ParameterParsing;

/// <summary>
/// Interface for extracting components from a parameter block.
/// </summary>
public interface IParameterComponentExtractor
{
    /// <summary>
    /// Extracts type, name, default value, and comments from a parameter block.
    /// </summary>
    /// <param name="block">The parameter block to parse</param>
    /// <returns>A fully parsed parameter with all components extracted</returns>
    CppParameter ExtractComponents(ParameterBlock block);
}
