using Xunit;
using CppToCsConverter.Core.Parsers.ParameterParsing;

namespace CppToCsConverter.Tests.ParameterParsing;

/// <summary>
/// Tests for the real-world bug that motivated the parameter parser refactoring.
/// Bug: Method matching failed due to whitespace differences between header and source.
/// Header: void CreateResRelRateIndex(CAgrMT* pmtTable, agrint& mtIndex);
/// Source: void CAgrLibHS::CreateResRelRateIndex(CAgrMT *pmtTable, agrint &mtIndex)
/// </summary>
public class RealScenarioBugTests
{
    private readonly CppParameterParser _parser;

    public RealScenarioBugTests()
    {
        _parser = new CppParameterParser(
            new ParameterBlockSplitter(),
            new ParameterComponentExtractor()
        );
    }

    [Fact]
    public void CreateResRelRateIndex_HeaderAndSourceSignatures_Match()
    {
        // Header version: pointer attached to type, reference attached to type
        var headerParams = "CAgrMT* pmtTable, agrint& mtIndex";
        
        // Source version: pointer has space before it, reference has space before it
        var sourceParams = "CAgrMT *pmtTable, agrint &mtIndex";
        
        var headerParsed = _parser.ParseParameters(headerParams);
        var sourceParsed = _parser.ParseParameters(sourceParams);
        
        // Should have same number of parameters
        Assert.Equal(2, headerParsed.Count);
        Assert.Equal(2, sourceParsed.Count);
        
        // First parameter: CAgrMT* pmtTable vs CAgrMT *pmtTable
        Assert.Equal("CAgrMT", headerParsed[0].Type);
        Assert.True(headerParsed[0].IsPointer);
        Assert.Equal("pmtTable", headerParsed[0].Name);
        Assert.Equal("CAgrMT", sourceParsed[0].Type);
        Assert.True(sourceParsed[0].IsPointer);
        Assert.Equal("pmtTable", sourceParsed[0].Name);
        
        // CRITICAL: Canonical signatures must match despite whitespace differences
        Assert.Equal(headerParsed[0].CanonicalSignature, sourceParsed[0].CanonicalSignature);
        
        // Second parameter: agrint& mtIndex vs agrint &mtIndex
        Assert.Equal("agrint", headerParsed[1].Type);
        Assert.True(headerParsed[1].IsReference);
        Assert.Equal("mtIndex", headerParsed[1].Name);
        Assert.Equal("agrint", sourceParsed[1].Type);
        Assert.True(sourceParsed[1].IsReference);
        Assert.Equal("mtIndex", sourceParsed[1].Name);
        
        // CRITICAL: Canonical signatures must match despite whitespace differences
        Assert.Equal(headerParsed[1].CanonicalSignature, sourceParsed[1].CanonicalSignature);
    }
    
    [Theory]
    [InlineData("CAgrMT* pmtTable", "CAgrMT *pmtTable")]
    [InlineData("CAgrMT* pmtTable", "CAgrMT  *  pmtTable")]
    [InlineData("const agrint& value", "const agrint & value")]
    [InlineData("agrint const& value", "const agrint & value")]
    [InlineData("TAttId const * pId", "const TAttId* pId")]
    public void ParameterVariations_CanonicalSignatures_Match(string param1, string param2)
    {
        var parsed1 = _parser.ParseParameters(param1);
        var parsed2 = _parser.ParseParameters(param2);
        
        Assert.Single(parsed1);
        Assert.Single(parsed2);
        
        // Despite different whitespace and const positioning, canonical signatures should match
        Assert.Equal(parsed1[0].CanonicalSignature, parsed2[0].CanonicalSignature);
    }
    
    [Fact]
    public void ComplexMethod_HeaderAndSource_CanonicalSignaturesMatch()
    {
        // Real-world example with multiple parameters, const variations, spacing variations
        var headerParams = "CAgrMT* pmtTable, const TDimValue& dimValue, bool bFlag = true";
        var sourceParams = "CAgrMT *pmtTable, TDimValue const & dimValue, bool bFlag";
        
        var headerParsed = _parser.ParseParameters(headerParams);
        var sourceParsed = _parser.ParseParameters(sourceParams);
        
        Assert.Equal(3, headerParsed.Count);
        Assert.Equal(3, sourceParsed.Count);
        
        // All canonical signatures should match
        for (int i = 0; i < 3; i++)
        {
            Assert.Equal(headerParsed[i].CanonicalSignature, sourceParsed[i].CanonicalSignature);
        }
    }
}
