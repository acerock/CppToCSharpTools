using Xunit;
using CppToCsConverter.Core.Parsers.ParameterParsing;
using CppToCsConverter.Core.Models;

namespace CppToCsConverter.Tests.ParameterParsing;

/// <summary>
/// Tests to verify that comment position information (Prefix vs Suffix) is preserved
/// through the entire parsing and conversion pipeline.
/// </summary>
public class CommentPositionPreservationTests
{
    private readonly CppParameterParser _parser;

    public CommentPositionPreservationTests()
    {
        _parser = new CppParameterParser(
            new ParameterBlockSplitter(),
            new ParameterComponentExtractor()
        );
    }

    [Fact]
    public void PrefixComment_PositionIsPreserved()
    {
        var paramText = "/* prefix comment */ const agrint& value";
        
        var cppParams = _parser.ParseParameters(paramText);
        
        Assert.Single(cppParams);
        Assert.Single(cppParams[0].PositionedComments);
        Assert.Equal(CommentPosition.Prefix, cppParams[0].PositionedComments[0].Position);
        Assert.Contains("prefix comment", cppParams[0].PositionedComments[0].CommentText);
    }

    [Fact]
    public void SuffixComment_PositionIsPreserved()
    {
        var paramText = "const agrint& value /* suffix comment */";
        
        var cppParams = _parser.ParseParameters(paramText);
        
        Assert.Single(cppParams);
        Assert.Single(cppParams[0].PositionedComments);
        Assert.Equal(CommentPosition.Suffix, cppParams[0].PositionedComments[0].Position);
        Assert.Contains("suffix comment", cppParams[0].PositionedComments[0].CommentText);
    }

    [Fact]
    public void MultiplePrefixAndSuffixComments_AllPositionsPreserved()
    {
        var paramText = "/* prefix 1 */ /* prefix 2 */ CAgrMT* pmtTable /* suffix 1 */ // suffix 2";
        
        var cppParams = _parser.ParseParameters(paramText);
        
        Assert.Single(cppParams);
        Assert.Equal(4, cppParams[0].PositionedComments.Count);
        
        // First two should be prefix
        Assert.Equal(CommentPosition.Prefix, cppParams[0].PositionedComments[0].Position);
        Assert.Contains("prefix 1", cppParams[0].PositionedComments[0].CommentText);
        
        Assert.Equal(CommentPosition.Prefix, cppParams[0].PositionedComments[1].Position);
        Assert.Contains("prefix 2", cppParams[0].PositionedComments[1].CommentText);
        
        // Last two should be suffix
        Assert.Equal(CommentPosition.Suffix, cppParams[0].PositionedComments[2].Position);
        Assert.Contains("suffix 1", cppParams[0].PositionedComments[2].CommentText);
        
        Assert.Equal(CommentPosition.Suffix, cppParams[0].PositionedComments[3].Position);
        Assert.Contains("suffix 2", cppParams[0].PositionedComments[3].CommentText);
    }

    [Fact]
    public void MultipleParameters_EachHasCorrectCommentPositions()
    {
        var paramText = "/* p1 prefix */ CAgrMT* pmtTable /* p1 suffix */, /* p2 prefix */ agrint& value // p2 suffix";
        
        var cppParams = _parser.ParseParameters(paramText);
        
        Assert.Equal(2, cppParams.Count);
        
        // First parameter
        Assert.Equal(2, cppParams[0].PositionedComments.Count);
        Assert.Equal(CommentPosition.Prefix, cppParams[0].PositionedComments[0].Position);
        Assert.Equal(CommentPosition.Suffix, cppParams[0].PositionedComments[1].Position);
        
        // Second parameter
        Assert.Equal(2, cppParams[1].PositionedComments.Count);
        Assert.Equal(CommentPosition.Prefix, cppParams[1].PositionedComments[0].Position);
        Assert.Equal(CommentPosition.Suffix, cppParams[1].PositionedComments[1].Position);
    }
}
