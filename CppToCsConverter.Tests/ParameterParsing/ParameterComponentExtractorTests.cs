using Xunit;
using CppToCsConverter.Core.Parsers.ParameterParsing;
using CppToCsConverter.Core.Models;

namespace CppToCsConverter.Tests.ParameterParsing;

/// <summary>
/// Tests for the ParameterComponentExtractor - ensures it correctly extracts
/// type, name, default value, and comments from parameter blocks.
/// </summary>
public class ParameterComponentExtractorTests
{
    private readonly IParameterComponentExtractor _extractor;

    public ParameterComponentExtractorTests()
    {
        _extractor = new ParameterComponentExtractor();
    }

    [Fact]
    public void ExtractComponents_SimpleParameter_ExtractsTypeAndName()
    {
        var block = new ParameterBlock("int value", 0, false, 0);
        var result = _extractor.ExtractComponents(block);
        
        Assert.Equal("int", result.Type);
        Assert.Equal("value", result.Name);
        Assert.Null(result.DefaultValue);
        Assert.Empty(result.PositionedComments);
    }

    [Fact]
    public void ExtractComponents_TrailingComma_HandlesCorrectly()
    {
        var block = new ParameterBlock("CAgrMT* pmtTable,", 0, false, 0);
        var result = _extractor.ExtractComponents(block);
        
        Assert.Equal("CAgrMT", result.Type);
        Assert.True(result.IsPointer);
        Assert.Equal("pmtTable", result.Name);
    }

    [Fact]
    public void ExtractComponents_ReferenceParameter_ExtractsCorrectly()
    {
        var block = new ParameterBlock("agrint& mtIndex", 0, false, 0);
        var result = _extractor.ExtractComponents(block);
        
        Assert.Equal("agrint", result.Type);
        Assert.True(result.IsReference);
        Assert.Equal("mtIndex", result.Name);
    }

    [Theory]
    [InlineData("const TAttId& attId")]
    [InlineData("const TAttId &attId")]
    [InlineData("const TAttId  &  attId")]
    [InlineData("TAttId const& attId")]
    [InlineData("TAttId const & attId")]
    public void ExtractComponents_ConstVariations_NormalizesType(string input)
    {
        var block = new ParameterBlock(input, 0, false, 0);
        var result = _extractor.ExtractComponents(block);
        
        // Type should be the base type, modifiers in flags
        Assert.Equal("TAttId", result.Type);
        Assert.True(result.IsConst);
        Assert.True(result.IsReference);
        Assert.Equal("attId", result.Name);
    }

    [Theory]
    [InlineData("CAgrMT* pmtTable")]
    [InlineData("CAgrMT *pmtTable")]
    [InlineData("CAgrMT  *  pmtTable")]
    public void ExtractComponents_PointerSpacingVariations_ExtractsCorrectly(string input)
    {
        var block = new ParameterBlock(input, 0, false, 0);
        var result = _extractor.ExtractComponents(block);
        
        Assert.Equal("CAgrMT", result.Type);
        Assert.True(result.IsPointer);
        Assert.Equal("pmtTable", result.Name);
    }

    [Fact]
    public void ExtractComponents_ParameterWithDefaultValue_ExtractsDefault()
    {
        var block = new ParameterBlock("int value = 0", 0, false, 0);
        var result = _extractor.ExtractComponents(block);
        
        Assert.Equal("int", result.Type);
        Assert.Equal("value", result.Name);
        Assert.Equal("0", result.DefaultValue);
    }

    [Fact]
    public void ExtractComponents_ComplexDefaultValue_ExtractsCorrectly()
    {
        var block = new ParameterBlock("CString cParam = _T(\"\")", 0, false, 0);
        var result = _extractor.ExtractComponents(block);
        
        Assert.Equal("CString", result.Type);
        Assert.Equal("cParam", result.Name);
        Assert.Equal("_T(\"\")", result.DefaultValue);
    }

    [Fact]
    public void ExtractComponents_BoolDefaultValue_ExtractsCorrectly()
    {
        var block = new ParameterBlock("bool bFlag = false", 0, false, 0);
        var result = _extractor.ExtractComponents(block);
        
        Assert.Equal("bool", result.Type);
        Assert.Equal("bFlag", result.Name);
        Assert.Equal("false", result.DefaultValue);
    }

    [Fact]
    public void ExtractComponents_PrefixComment_ClassifiesCorrectly()
    {
        var block = new ParameterBlock("/* IN */ const agrint& value", 0, false, 0);
        var result = _extractor.ExtractComponents(block);
        
        var prefixComments = result.PositionedComments.Where(c => c.Position == CommentPosition.Prefix).ToList();
        Assert.Single(prefixComments);
        Assert.Equal(CommentPosition.Prefix, prefixComments[0].Position);
        Assert.Contains("/* IN */", prefixComments[0].CommentText);
        Assert.Equal("agrint", result.Type);
        Assert.True(result.IsConst);
        Assert.True(result.IsReference);
        Assert.Equal("value", result.Name);
    }

    [Fact]
    public void ExtractComponents_SuffixCStyleComment_ClassifiesCorrectly()
    {
        var block = new ParameterBlock("const agrint& value /* IN */", 0, false, 0);
        var result = _extractor.ExtractComponents(block);
        
        var suffixComments = result.PositionedComments.Where(c => c.Position == CommentPosition.Suffix).ToList();
        Assert.Single(suffixComments);
        Assert.Equal(CommentPosition.Suffix, suffixComments[0].Position);
        Assert.Contains("/* IN */", suffixComments[0].CommentText);
    }

    [Fact]
    public void ExtractComponents_SuffixCppComment_ClassifiesCorrectly()
    {
        var block = new ParameterBlock("bool bAnalysis, // Comment 1\n", 0, false, 0);
        var result = _extractor.ExtractComponents(block);
        
        var suffixComments = result.PositionedComments.Where(c => c.Position == CommentPosition.Suffix).ToList();
        Assert.Single(suffixComments);
        Assert.Equal(CommentPosition.Suffix, suffixComments[0].Position);
        Assert.Contains("// Comment 1", suffixComments[0].CommentText);
        Assert.Equal("bool", result.Type);
        Assert.Equal("bAnalysis", result.Name);
    }

    [Fact]
    public void ExtractComponents_TrailingCommaWithComment_HandlesCorrectly()
    {
        var block = new ParameterBlock("CAgrMT* pmtTrans, /*IN/OUT: Memory table */\n", 0, false, 0);
        var result = _extractor.ExtractComponents(block);
        
        Assert.Equal("CAgrMT", result.Type);
        Assert.True(result.IsPointer);
        Assert.Equal("pmtTrans", result.Name);
        var suffixComments = result.PositionedComments.Where(c => c.Position == CommentPosition.Suffix).ToList();
        Assert.Single(suffixComments);
        Assert.Contains("/*IN/OUT: Memory table */", suffixComments[0].CommentText);
    }

    [Fact]
    public void ExtractComponents_ParameterWithoutName_ReturnsEmptyName()
    {
        // Legal in C++ headers: parameter type without name
        var block = new ParameterBlock("const CString&", 0, false, 0);
        var result = _extractor.ExtractComponents(block);
        
        Assert.Equal("CString", result.Type);
        Assert.True(result.IsConst);
        Assert.True(result.IsReference);
        Assert.Empty(result.Name);
    }

    [Fact]
    public void ExtractComponents_ComplexRealWorld_ParsesCorrectly()
    {
        var block = new ParameterBlock("const TDimValue& dimValueId, /*IN: Value reference */\n", 0, true, 8);
        var result = _extractor.ExtractComponents(block);
        
        Assert.Equal("TDimValue", result.Type);
        Assert.True(result.IsConst);
        Assert.True(result.IsReference);
        Assert.Equal("dimValueId", result.Name);
        Assert.True(result.HasLineBreak);
        Assert.Equal(8, result.OriginalIndent);
        var suffixComments = result.PositionedComments.Where(c => c.Position == CommentPosition.Suffix).ToList();
        Assert.Single(suffixComments);
        Assert.Contains("/*IN: Value reference */", suffixComments[0].CommentText);
    }

    [Fact]
    public void ExtractComponents_LineBreakAndIndent_PreservesFormatting()
    {
        var block = new ParameterBlock("        const agrint& lIdRes", 0, true, 8);
        var result = _extractor.ExtractComponents(block);
        
        Assert.True(result.HasLineBreak);
        Assert.Equal(8, result.OriginalIndent);
    }

    [Fact]
    public void ExtractComponents_CanonicalSignature_NormalizesCorrectly()
    {
        var block = new ParameterBlock("const TAttId  &  attId", 0, false, 0);
        var result = _extractor.ExtractComponents(block);
        
        // Canonical signature should have normalized whitespace
        Assert.DoesNotContain("  ", result.CanonicalSignature);
        Assert.Contains("const", result.CanonicalSignature);
        Assert.Contains("TAttId", result.CanonicalSignature);
        Assert.Contains("&", result.CanonicalSignature);
    }

    [Theory]
    [InlineData("CAgrMT* pmtTable", "CAgrMT *pmtTable")]
    [InlineData("const TAttId& attId", "TAttId const & attId")]
    [InlineData("agrint &value", "agrint& value")]
    public void ExtractComponents_CanonicalSignature_MatchesDespiteWhitespace(string input1, string input2)
    {
        var block1 = new ParameterBlock(input1, 0, false, 0);
        var block2 = new ParameterBlock(input2, 0, false, 0);
        
        var result1 = _extractor.ExtractComponents(block1);
        var result2 = _extractor.ExtractComponents(block2);
        
        // Canonical signatures should match
        Assert.Equal(result1.CanonicalSignature, result2.CanonicalSignature);
    }

    [Fact]
    public void ExtractComponents_CanonicalSignature_IgnoresComments()
    {
        var block1 = new ParameterBlock("const agrint& value", 0, false, 0);
        var block2 = new ParameterBlock("/* IN */ const agrint& value /* some comment */", 0, false, 0);
        
        var result1 = _extractor.ExtractComponents(block1);
        var result2 = _extractor.ExtractComponents(block2);
        
        // Canonical signatures should match (comments ignored)
        Assert.Equal(result1.CanonicalSignature, result2.CanonicalSignature);
    }

    [Fact]
    public void ExtractComponents_TemplateType_PreservesAngleBrackets()
    {
        var block = new ParameterBlock("vector<pair<int, int>> data", 0, false, 0);
        var result = _extractor.ExtractComponents(block);
        
        Assert.Contains("vector<pair<int, int>>", result.Type);
        Assert.Equal("data", result.Name);
    }

    [Fact]
    public void ExtractComponents_ArrayParameter_ExtractsCorrectly()
    {
        var block = new ParameterBlock("char buffer[256]", 0, false, 0);
        var result = _extractor.ExtractComponents(block);
        
        Assert.Contains("char", result.Type);
        Assert.Equal("buffer", result.Name);
        // Array size should be part of the name or handled specially
    }

    [Fact]
    public void ExtractComponents_MultiplePrefixComments_CapturesAll()
    {
        var block = new ParameterBlock("/* comment1 */ /* comment2 */ int value", 0, false, 0);
        var result = _extractor.ExtractComponents(block);
        
        var prefixComments = result.PositionedComments.Where(c => c.Position == CommentPosition.Prefix).ToList();
        Assert.Equal(2, prefixComments.Count);
    }

    [Fact]
    public void ExtractComponents_MixedComments_ClassifiesCorrectly()
    {
        var block = new ParameterBlock("/* prefix */ const agrint& value /* suffix */", 0, false, 0);
        var result = _extractor.ExtractComponents(block);
        
        var prefixComments = result.PositionedComments.Where(c => c.Position == CommentPosition.Prefix).ToList();
        var suffixComments = result.PositionedComments.Where(c => c.Position == CommentPosition.Suffix).ToList();
        Assert.Single(prefixComments);
        Assert.Single(suffixComments);
        Assert.Contains("prefix", prefixComments[0].CommentText);
        Assert.Contains("suffix", suffixComments[0].CommentText);
    }
}
