namespace Radzen.Blazor.Spreadsheet.Tests;

using System;
using Xunit;

public class FormulaLexerTests
{
    [Fact]
    public void FormulaLexer_ShouldParseCellIdentifier()
    {
        var tokens = FormulaLexer.Scan("=A1");
        Assert.Equal(FormulaTokenType.Equals, tokens[0].Type);
        Assert.Equal(0, tokens[0].Start);
        Assert.Equal(1, tokens[0].End);
        Assert.Equal(FormulaTokenType.CellIdentifier, tokens[1].Type);
        Assert.Equal("A1", tokens[1].Address.ToString());
        Assert.Equal(1, tokens[1].Start);
        Assert.Equal(3, tokens[1].End);
    }

    [Fact]
    public void FormulaLexer_ShouldParseSimpleFormula()
    {
        var tokens = FormulaLexer.Scan("=A1+b2");
        Assert.Equal(FormulaTokenType.Equals, tokens[0].Type);
        Assert.Equal(0, tokens[0].Start);
        Assert.Equal(1, tokens[0].End);
        Assert.Equal(FormulaTokenType.CellIdentifier, tokens[1].Type);
        Assert.Equal("A1", tokens[1].Value);
        Assert.Equal(1, tokens[1].Start);
        Assert.Equal(3, tokens[1].End);
        Assert.Equal(FormulaTokenType.Plus, tokens[2].Type);
        Assert.Equal(3, tokens[2].Start);
        Assert.Equal(4, tokens[2].End);
        Assert.Equal(FormulaTokenType.CellIdentifier, tokens[3].Type);
        Assert.Equal("b2", tokens[3].Value);
        Assert.Equal(4, tokens[3].Start);
        Assert.Equal(6, tokens[3].End);
    }

    [Fact]
    public void FormulaLexer_ShouldPreserveWhitespaceAsTrivia()
    {
        var tokens = FormulaLexer.Scan("= A1 + b2 ");

        // Check that whitespace is preserved as trivia
        Assert.Equal(FormulaTokenType.Equals, tokens[0].Type);
        Assert.Empty(tokens[0].LeadingTrivia);
        Assert.Single(tokens[0].TrailingTrivia);
        Assert.Equal(FormulaTokenTriviaKind.Whitespace, tokens[0].TrailingTrivia[0].Kind);
        Assert.Equal(" ", tokens[0].TrailingTrivia[0].Text);
        Assert.Equal(0, tokens[0].Start);
        Assert.Equal(2, tokens[0].End);

        Assert.Equal(FormulaTokenType.CellIdentifier, tokens[1].Type);
        Assert.Empty(tokens[1].LeadingTrivia);
        Assert.Single(tokens[1].TrailingTrivia);
        Assert.Equal(FormulaTokenTriviaKind.Whitespace, tokens[1].TrailingTrivia[0].Kind);
        Assert.Equal(" ", tokens[1].TrailingTrivia[0].Text);
        Assert.Equal(2, tokens[1].Start);
        Assert.Equal(5, tokens[1].End);

        Assert.Equal(FormulaTokenType.Plus, tokens[2].Type);
        Assert.Empty(tokens[2].LeadingTrivia);
        Assert.Single(tokens[2].TrailingTrivia);
        Assert.Equal(FormulaTokenTriviaKind.Whitespace, tokens[2].TrailingTrivia[0].Kind);
        Assert.Equal(" ", tokens[2].TrailingTrivia[0].Text);
        Assert.Equal(5, tokens[2].Start);
        Assert.Equal(7, tokens[2].End);

        Assert.Equal(FormulaTokenType.CellIdentifier, tokens[3].Type);
        Assert.Empty(tokens[3].LeadingTrivia);
        Assert.Single(tokens[3].TrailingTrivia);
        Assert.Equal(FormulaTokenTriviaKind.Whitespace, tokens[3].TrailingTrivia[0].Kind);
        Assert.Equal(" ", tokens[3].TrailingTrivia[0].Text);
        Assert.Equal(7, tokens[3].Start);
        Assert.Equal(10, tokens[3].End);
    }

    [Fact]
    public void FormulaLexer_ShouldPreserveMultipleWhitespaceAsTrivia()
    {
        var tokens = FormulaLexer.Scan("=  A1  +  b2  ");

        // Check that multiple whitespace characters are preserved
        Assert.Equal(FormulaTokenType.Equals, tokens[0].Type);
        Assert.Single(tokens[0].TrailingTrivia);
        Assert.Equal("  ", tokens[0].TrailingTrivia[0].Text);
        Assert.Equal(0, tokens[0].Start);
        Assert.Equal(3, tokens[0].End);

        Assert.Equal(FormulaTokenType.CellIdentifier, tokens[1].Type);
        Assert.Single(tokens[1].TrailingTrivia);
        Assert.Equal("  ", tokens[1].TrailingTrivia[0].Text);
        Assert.Equal(3, tokens[1].Start);
        Assert.Equal(7, tokens[1].End);

        Assert.Equal(FormulaTokenType.Plus, tokens[2].Type);
        Assert.Single(tokens[2].TrailingTrivia);
        Assert.Equal("  ", tokens[2].TrailingTrivia[0].Text);
        Assert.Equal(7, tokens[2].Start);
        Assert.Equal(10, tokens[2].End);

        Assert.Equal(FormulaTokenType.CellIdentifier, tokens[3].Type);
        Assert.Single(tokens[3].TrailingTrivia);
        Assert.Equal("  ", tokens[3].TrailingTrivia[0].Text);
        Assert.Equal(10, tokens[3].Start);
        Assert.Equal(14, tokens[3].End);
    }

    [Fact]
    public void FormulaLexer_ShouldPreserveLineEndingsAsTrivia()
    {
        var tokens = FormulaLexer.Scan("=A1\n+b2");

        // Check that line endings are preserved as trivia
        Assert.Equal(FormulaTokenType.Equals, tokens[0].Type);
        Assert.Empty(tokens[0].LeadingTrivia);
        Assert.Empty(tokens[0].TrailingTrivia);
        Assert.Equal(0, tokens[0].Start);
        Assert.Equal(1, tokens[0].End);

        Assert.Equal(FormulaTokenType.CellIdentifier, tokens[1].Type);
        Assert.Empty(tokens[1].LeadingTrivia);
        Assert.Single(tokens[1].TrailingTrivia);
        Assert.Equal(FormulaTokenTriviaKind.EndOfLine, tokens[1].TrailingTrivia[0].Kind);
        Assert.Equal("\n", tokens[1].TrailingTrivia[0].Text);
        Assert.Equal(1, tokens[1].Start);
        Assert.Equal(4, tokens[1].End);

        Assert.Equal(FormulaTokenType.Plus, tokens[2].Type);
        Assert.Empty(tokens[2].LeadingTrivia);
        Assert.Empty(tokens[2].TrailingTrivia);
        Assert.Equal(4, tokens[2].Start);
        Assert.Equal(5, tokens[2].End);

        Assert.Equal(expected: FormulaTokenType.CellIdentifier, tokens[3].Type);
        Assert.Empty(tokens[3].LeadingTrivia);
        Assert.Empty(tokens[3].TrailingTrivia);
        Assert.Equal(5, tokens[3].Start);
        Assert.Equal(7, tokens[3].End);
    }
}