namespace Radzen.Blazor.Spreadsheet.Tests;

using Xunit;

public class FormulaLexerTests
{
    [Fact]
    public void FormulaLexer_ShouldParseCellIdentifier()
    {
        var tokens = FormulaLexer.Scan("=A1");
        Assert.Equal(FormulaTokenType.Equals, tokens[0].Type);
        Assert.Equal(FormulaTokenType.CellIdentifier, tokens[1].Type);
        Assert.Equal("A1", tokens[1].AddressValue.ToString());
    }

    [Fact]
    public void FormulaLexer_ShouldParseSimpleFormula()
    {
        var tokens = FormulaLexer.Scan("=A1+b2");
        Assert.Equal(FormulaTokenType.Equals, tokens[0].Type);
        Assert.Equal(FormulaTokenType.CellIdentifier, tokens[1].Type);
        Assert.Equal("A1", tokens[1].Value);
        Assert.Equal(FormulaTokenType.Plus, tokens[2].Type);
        Assert.Equal(FormulaTokenType.CellIdentifier, tokens[3].Type);
        Assert.Equal("b2", tokens[3].Value);
    }
}