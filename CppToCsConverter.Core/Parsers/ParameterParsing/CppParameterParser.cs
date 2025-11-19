namespace CppToCsConverter.Core.Parsers.ParameterParsing;

using CppToCsConverter.Core.Models;

/// <summary>
/// Main entry point for parsing C++ parameter lists.
/// Orchestrates the block splitting and component extraction phases.
/// </summary>
public class CppParameterParser
{
    private readonly IParameterBlockSplitter _blockSplitter;
    private readonly IParameterComponentExtractor _componentExtractor;

    public CppParameterParser(
        IParameterBlockSplitter blockSplitter,
        IParameterComponentExtractor componentExtractor)
    {
        _blockSplitter = blockSplitter ?? throw new ArgumentNullException(nameof(blockSplitter));
        _componentExtractor = componentExtractor ?? throw new ArgumentNullException(nameof(componentExtractor));
    }

    /// <summary>
    /// Parses a complete C++ parameter list into structured parameters.
    /// </summary>
    /// <param name="parameterListText">The parameter list text (everything between method's outer parentheses)</param>
    /// <returns>A list of fully parsed CppParameter objects</returns>
    public List<CppParameter> ParseParameters(string parameterListText)
    {
        if (string.IsNullOrWhiteSpace(parameterListText))
            return new List<CppParameter>();

        // Phase 1: Split into blocks
        var blocks = _blockSplitter.SplitIntoBlocks(parameterListText);

        // Phase 2: Extract components from each block
        var parameters = new List<CppParameter>();
        foreach (var block in blocks)
        {
            var parameter = _componentExtractor.ExtractComponents(block);
            parameters.Add(parameter);
        }

        return parameters;
    }
}
