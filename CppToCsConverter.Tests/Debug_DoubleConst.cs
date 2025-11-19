using Xunit;
using Xunit.Abstractions;
using CppToCsConverter.Core.Parsers.ParameterParsing;

namespace CppToCsConverter.Tests;

public class Debug_DoubleConst
{
    private readonly ITestOutputHelper _output;
    private readonly CppParameterParser _parser;

    public Debug_DoubleConst(ITestOutputHelper output)
    {
        _output = output;
        _parser = new CppParameterParser(
            new ParameterBlockSplitter(),
            new ParameterComponentExtractor()
        );
    }

    [Fact]
    public void Debug_ParseParameter_FromTrickyToMatch()
    {
        // This is the parameter string from the .cpp file
        var paramString = "/* IN*/const CString& cResTab";

        var results = _parser.ParseParameters(paramString);

        Assert.Single(results);
        var param = results[0];

        _output.WriteLine($"Input: '{paramString}'");
        _output.WriteLine($"Type: '{param.Type}'");
        _output.WriteLine($"Name: '{param.Name}'");
        _output.WriteLine($"IsConst: {param.IsConst}");
        _output.WriteLine($"IsPointer: {param.IsPointer}");
        _output.WriteLine($"IsReference: {param.IsReference}");
        _output.WriteLine($"PositionedComments: {param.PositionedComments.Count}");
        foreach (var comment in param.PositionedComments)
        {
            _output.WriteLine($"  - '{comment.CommentText}' ({comment.Position})");
        }
        _output.WriteLine($"OriginalText: '{param.OriginalText}'");
        _output.WriteLine($"CanonicalSignature: '{param.CanonicalSignature}'");

        // Verify the extracted properties
        Assert.Equal("CString", param.Type); // Just base type, no modifiers
        Assert.Equal("cResTab", param.Name);
        Assert.True(param.IsConst);
        Assert.False(param.IsPointer);
        Assert.True(param.IsReference);
        Assert.Single(param.PositionedComments);
    }
}
