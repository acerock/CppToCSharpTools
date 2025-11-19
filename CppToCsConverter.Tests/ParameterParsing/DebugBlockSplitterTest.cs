using CppToCsConverter.Core.Parsers.ParameterParsing;

namespace CppToCsConverter.Tests.ParameterParsing;

public class DebugBlockSplitterTest
{
    [Fact]
    public void Debug_CppStyleComment()
    {
        var splitter = new ParameterBlockSplitter();
        var result = splitter.SplitIntoBlocks("bool bAnalysis, // Comment 1\nbool bRate");
        
        Assert.Equal(2, result.Count);
        
        // Debug output
        for (int i = 0; i < result.Count; i++)
        {
            Console.WriteLine($"Block {i}: '{result[i].RawText}'");
        }
    }
    
    [Fact]
    public void Debug_CStyleComment()
    {
        var splitter = new ParameterBlockSplitter();
        var result = splitter.SplitIntoBlocks("CAgrMT* pmtTrans, /*IN/OUT: Memory table */\ndouble& dValue");
        
        Assert.Equal(2, result.Count);
        
        // Debug output
        for (int i = 0; i < result.Count; i++)
        {
            Console.WriteLine($"Block {i}: '{result[i].RawText}'");
        }
    }
}
