using Xunit;
using CppToCsConverter.Core.Parsers.ParameterParsing;
using CppToCsConverter.Core.Models;

namespace CppToCsConverter.Tests.ParameterParsing;

public class DebugCommentCountTests
{
    [Fact]
    public void Debug_BlockSplitter_ComplexRealWorld()
    {
        var splitter = new ParameterBlockSplitter();
        
        var input = @"CAgrMT* pmtTrans, /*IN/OUT: Memory table */
        double& dValue, /*OUT: Return value */
        const TDimValue& dimValueId, /*IN: Value reference */
        CString& cTransDateFrom,
        const double& dPostFlag";
        
        var result = splitter.SplitIntoBlocks(input);
        
        System.Console.WriteLine($"Total blocks: {result.Count}");
        for (int i = 0; i < result.Count; i++)
        {
            System.Console.WriteLine($"Block {i}: '{result[i].RawText}'");
            System.Console.WriteLine($"  StartsOnNewLine: {result[i].StartsOnNewLine}");
        }
        
        Assert.Equal(5, result.Count);
    }

    [Fact]
    public void Debug_CommentCounting()
    {
        var splitter = new ParameterBlockSplitter();
        var extractor = new ParameterComponentExtractor();
        var parser = new CppParameterParser(splitter, extractor);

        var paramText = "/* p1 prefix */ CAgrMT* pmtTable /* p1 suffix */, /* p2 prefix */ agrint& value // p2 suffix";

        // Debug blocks
        var blocks = splitter.SplitIntoBlocks(paramText);
        System.Console.WriteLine($"Total blocks: {blocks.Count}");
        for (int i = 0; i < blocks.Count; i++)
        {
            System.Console.WriteLine($"Block {i}: '{blocks[i].RawText}'");
        }

        var cppParams = parser.ParseParameters(paramText);

        // Debug output
        for (int i = 0; i < cppParams.Count; i++)
        {
            var param = cppParams[i];
            System.Console.WriteLine($"\nParameter {i}: {param.Type} {param.Name}");
            System.Console.WriteLine($"  PositionedComments count: {param.PositionedComments.Count}");
            foreach (var comment in param.PositionedComments)
            {
                System.Console.WriteLine($"    - {comment.Position}: '{comment.CommentText}'");
            }
        }
        
        // This should fail if there's a problem
        Assert.Equal(2, cppParams[1].PositionedComments.Count);
    }
}
