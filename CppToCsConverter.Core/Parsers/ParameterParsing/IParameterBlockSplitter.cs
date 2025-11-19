namespace CppToCsConverter.Core.Parsers.ParameterParsing;

/// <summary>
/// Interface for splitting a C++ parameter list string into individual parameter blocks.
/// </summary>
public interface IParameterBlockSplitter
{
    /// <summary>
    /// Splits a C++ parameter list into individual parameter blocks.
    /// Respects comment context, parentheses depth, and angle bracket depth.
    /// </summary>
    /// <param name="parameterListText">The full parameter list text (everything between the method's outer parentheses)</param>
    /// <returns>A list of parameter blocks in order</returns>
    List<ParameterBlock> SplitIntoBlocks(string parameterListText);
}
