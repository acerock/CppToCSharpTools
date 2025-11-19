using Xunit;
using CppToCsConverter.Core.Parsers.ParameterParsing;

namespace CppToCsConverter.Tests.ParameterParsing;

/// <summary>
/// Tests for the ParameterBlockSplitter - ensures it correctly splits parameter lists
/// into individual blocks while respecting comments, parentheses, and templates.
/// </summary>
public class ParameterBlockSplitterTests
{
    private readonly IParameterBlockSplitter _splitter;

    public ParameterBlockSplitterTests()
    {
        _splitter = new ParameterBlockSplitter();
    }

    [Fact]
    public void SplitIntoBlocks_EmptyString_ReturnsEmptyList()
    {
        var result = _splitter.SplitIntoBlocks("");
        Assert.Empty(result);
    }

    [Fact]
    public void SplitIntoBlocks_WhitespaceOnly_ReturnsEmptyList()
    {
        var result = _splitter.SplitIntoBlocks("   \t\n  ");
        Assert.Empty(result);
    }

    [Fact]
    public void SplitIntoBlocks_SingleParameter_ReturnsSingleBlock()
    {
        var result = _splitter.SplitIntoBlocks("int value");
        
        Assert.Single(result);
        Assert.Equal("int value", result[0].RawText);
        Assert.Equal(0, result[0].Index);
    }

    [Fact]
    public void SplitIntoBlocks_TwoSimpleParameters_ReturnsTwoBlocks()
    {
        var result = _splitter.SplitIntoBlocks("int a, bool b");
        
        Assert.Equal(2, result.Count);
        // Note: RawText includes trailing comma/whitespace for complete reconstruction
        Assert.Contains("int a", result[0].RawText);
        Assert.Contains("bool b", result[1].RawText);
        Assert.Equal(0, result[0].Index);
        Assert.Equal(1, result[1].Index);
    }

    [Fact]
    public void SplitIntoBlocks_ThreeParameters_ReturnsThreeBlocks()
    {
        var result = _splitter.SplitIntoBlocks("const CString& cParam1, const bool &bParam2, CString *pcParam3");
        
        Assert.Equal(3, result.Count);
        Assert.Contains("const CString& cParam1", result[0].RawText);
        Assert.Contains("const bool &bParam2", result[1].RawText);
        Assert.Contains("CString *pcParam3", result[2].RawText);
    }

    [Theory]
    [InlineData("CAgrMT* pmtTable, agrint& mtIndex")]
    [InlineData("CAgrMT *pmtTable, agrint &mtIndex")]
    [InlineData("CAgrMT    *    pmtTable    ,    agrint    &    mtIndex")]
    public void SplitIntoBlocks_DifferentWhitespacing_SplitsCorrectly(string input)
    {
        var result = _splitter.SplitIntoBlocks(input);
        
        Assert.Equal(2, result.Count);
        Assert.Contains("pmtTable", result[0].RawText);
        Assert.Contains("mtIndex", result[1].RawText);
    }

    [Fact]
    public void SplitIntoBlocks_CStyleCommentInParameter_DoesNotSplitOnCommentComma()
    {
        var result = _splitter.SplitIntoBlocks("const CString& param1 /* IN, OUT */, int param2");
        
        Assert.Equal(2, result.Count);
        Assert.Contains("/* IN, OUT */", result[0].RawText);
        Assert.Contains("param1", result[0].RawText);
        Assert.Contains("param2", result[1].RawText);
    }

    [Fact]
    public void SplitIntoBlocks_CppStyleCommentAtEnd_IncludesCommentInBlock()
    {
        var result = _splitter.SplitIntoBlocks("bool bAnalysis, // Comment 1\nbool bRate");
        
        Assert.Equal(2, result.Count);
        Assert.Contains("// Comment 1", result[0].RawText);
        Assert.Contains("bAnalysis", result[0].RawText);
    }

    [Fact]
    public void SplitIntoBlocks_MultilineWithComments_PreservesStructure()
    {
        var input = @"const TAttId& attId, // Post-comment
    /* Suffix comment */ agrint* i";
        
        var result = _splitter.SplitIntoBlocks(input);
        
        Assert.Equal(2, result.Count);
        Assert.Contains("// Post-comment", result[0].RawText);
        Assert.Contains("/* Suffix comment */", result[1].RawText);
    }

    [Fact]
    public void SplitIntoBlocks_ParametersOnNewLines_DetectsLineBreaks()
    {
        var input = @"const agrint& lIdRes,
        const TAttId& attRelId,
        bool bAnalysis";
        
        var result = _splitter.SplitIntoBlocks(input);
        
        Assert.Equal(3, result.Count);
        Assert.False(result[0].StartsOnNewLine); // First parameter doesn't start on new line
        Assert.True(result[1].StartsOnNewLine);
        Assert.True(result[2].StartsOnNewLine);
    }

    [Fact]
    public void SplitIntoBlocks_ParametersWithIndentation_CapturesIndent()
    {
        var input = @"const agrint& lIdRes,
        const TAttId& attRelId,
            bool bAnalysis";
        
        var result = _splitter.SplitIntoBlocks(input);
        
        Assert.Equal(3, result.Count);
        Assert.Equal(8, result[1].LeadingIndent); // 8 spaces
        Assert.Equal(12, result[2].LeadingIndent); // 12 spaces
    }

    [Fact]
    public void SplitIntoBlocks_DefaultValue_DoesNotSplitOnCommaInDefault()
    {
        var result = _splitter.SplitIntoBlocks("CString cParam = _T(\"a,b,c\"), int value = 0");
        
        Assert.Equal(2, result.Count);
        Assert.Contains("_T(\"a,b,c\")", result[0].RawText);
    }

    [Fact]
    public void SplitIntoBlocks_TemplateType_DoesNotSplitOnTemplateComma()
    {
        var result = _splitter.SplitIntoBlocks("vector<pair<int, int>> data, bool flag");
        
        Assert.Equal(2, result.Count);
        Assert.Contains("vector<pair<int, int>>", result[0].RawText);
        Assert.Contains("flag", result[1].RawText);
    }

    [Fact]
    public void SplitIntoBlocks_ComplexRealWorldExample_ParsesCorrectly()
    {
        var input = @"CAgrMT* pmtTrans, /*IN/OUT: Memory table */
        double& dValue, /*OUT: Return value */
        const TDimValue& dimValueId, /*IN: Value reference */
        CString& cTransDateFrom,
        const double& dPostFlag";
        
        var result = _splitter.SplitIntoBlocks(input);
        
        Assert.Equal(5, result.Count);
        Assert.Contains("pmtTrans", result[0].RawText);
        Assert.Contains("/*IN/OUT: Memory table */", result[0].RawText);
        Assert.Contains("dValue", result[1].RawText);
        Assert.Contains("/*OUT: Return value */", result[1].RawText);
        Assert.True(result[1].StartsOnNewLine);
    }

    [Fact]
    public void SplitIntoBlocks_TrailingComma_IgnoresTrailingComma()
    {
        var result = _splitter.SplitIntoBlocks("int a, bool b,");
        
        Assert.Equal(2, result.Count);
        Assert.Contains("int a", result[0].RawText);
        Assert.Contains("bool b", result[1].RawText);
    }

    [Fact]
    public void SplitIntoBlocks_ReadmeExample1_SplitsCorrectly()
    {
        // From readme.md: void Test(const TAttId &attId, agrint * i)
        var result = _splitter.SplitIntoBlocks("const TAttId &attId, agrint * i");
        
        Assert.Equal(2, result.Count);
        Assert.Contains("const TAttId &attId", result[0].RawText);
        Assert.Contains("agrint * i", result[1].RawText);
    }

    [Fact]
    public void SplitIntoBlocks_ReadmeExample2_WithComments()
    {
        // From readme.md with comments
        var input = @"const TAttId & attId, // Post-comment
    /* Suffix comment */ agrint * i";
        
        var result = _splitter.SplitIntoBlocks(input);
        
        Assert.Equal(2, result.Count);
        Assert.Contains("// Post-comment", result[0].RawText);
        Assert.Contains("/* Suffix comment */", result[1].RawText);
    }

    [Fact]
    public void SplitIntoBlocks_ReadmeExample3_CommaAfterComment()
    {
        // From readme: const TAttId & attId /* Post-comment */, agrint * i
        var input = "const TAttId & attId /*/* Post-comment */, agrint * i";
        
        var result = _splitter.SplitIntoBlocks(input);
        
        Assert.Equal(2, result.Count);
        Assert.Contains("attId", result[0].RawText);
        Assert.Contains("/*/* Post-comment */", result[0].RawText);
    }
}
