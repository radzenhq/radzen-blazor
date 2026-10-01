using System.Globalization;
using System.Reflection;
using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class WholeColumnAndRowSyntaxTests
{
    private static string Adjust(string formula, int rowDelta, int colDelta)
    {
        var method = typeof(Worksheet).GetMethod(
            "AdjustFormulaForCopy",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        return (string)method.Invoke(null, [formula, rowDelta, colDelta])!;
    }

    [Fact]
    public void Lexer_ColumnRange_ProducesColumnIdentifiers()
    {
        var tokens = FormulaLexer.Scan("=SUM(A:A)");

        Assert.Equal(FormulaTokenType.ColumnIdentifier, tokens[3].Type);
        Assert.Equal(FormulaTokenType.Colon, tokens[4].Type);
        Assert.Equal(FormulaTokenType.ColumnIdentifier, tokens[5].Type);
        Assert.Equal(0, tokens[3].Address.Column);
    }

    [Fact]
    public void Lexer_RowRange_ProducesRowIdentifiers()
    {
        var tokens = FormulaLexer.Scan("=SUM(4:5)");

        Assert.Equal(FormulaTokenType.RowIdentifier, tokens[3].Type);
        Assert.Equal(FormulaTokenType.RowIdentifier, tokens[5].Type);
        Assert.Equal(3, tokens[3].Address.Row);
        Assert.Equal(4, tokens[5].Address.Row);
    }

    [Fact]
    public void Lexer_SheetQualifiedAbsoluteColumns_KeepSheetAndAbsoluteFlags()
    {
        var tokens = FormulaLexer.Scan("=Data!$A:B");

        Assert.Equal(FormulaTokenType.ColumnIdentifier, tokens[1].Type);
        Assert.Equal("Data!$A", tokens[1].Value);
        Assert.Equal("Data", tokens[1].Address.Worksheet);
        Assert.True(tokens[1].Address.IsColumnAbsolute);
        Assert.False(tokens[3].Address.IsColumnAbsolute);
    }

    [Fact]
    public void Lexer_QuotedSheetRows_KeepSheetName()
    {
        var tokens = FormulaLexer.Scan("='My Data'!$4:$4");

        Assert.Equal(FormulaTokenType.RowIdentifier, tokens[1].Type);
        Assert.Equal("My Data", tokens[1].Address.Worksheet);
        Assert.True(tokens[1].Address.IsRowAbsolute);
        Assert.True(tokens[3].Address.IsRowAbsolute);
    }

    [Theory]
    [InlineData("=A1:A")]
    [InlineData("=A:A1")]
    [InlineData("=A:1")]
    [InlineData("=XFE:XFE")]
    [InlineData("=0:0")]
    [InlineData("=1048577:1048577")]
    public void Parser_InvalidColumnOrRowRange_HasErrors(string formula)
    {
        Assert.NotEmpty(FormulaParser.Parse(formula).Errors);
    }

    [Fact]
    public void Parser_ArithmeticOnNumbers_IsNotRowRange()
    {
        var tree = FormulaParser.Parse("=1+4");

        Assert.Empty(tree.Errors);
        Assert.IsType<BinaryExpressionSyntaxNode>(tree.Root);
    }

    [Fact]
    public void Parser_FunctionCall_IsNotColumnReference()
    {
        var tree = FormulaParser.Parse("=SUM(1,2)");

        Assert.Empty(tree.Errors);
        Assert.IsType<FunctionSyntaxNode>(tree.Root);
    }

    [Fact]
    public void Parser_BareColumnLetters_IsStillName()
    {
        var tree = FormulaParser.Parse("=A");

        Assert.Empty(tree.Errors);
        Assert.IsType<NameSyntaxNode>(tree.Root);
    }

    [Fact]
    public void Parser_ColumnRange_SpansEveryRow()
    {
        var range = Assert.IsType<RangeSyntaxNode>(FormulaParser.Parse("=B:C").Root);

        Assert.Equal(RangeKind.Columns, range.Kind);
        Assert.Equal(new CellRef(0, 1), range.Start.Token.Address);
        Assert.Equal(new CellRef(Worksheet.MaxRows - 1, 2), range.End.Token.Address);
    }

    [Fact]
    public void Parser_RowRange_SpansEveryColumn()
    {
        var range = Assert.IsType<RangeSyntaxNode>(FormulaParser.Parse("=4:5").Root);

        Assert.Equal(RangeKind.Rows, range.Kind);
        Assert.Equal(new CellRef(3, 0), range.Start.Token.Address);
        Assert.Equal(new CellRef(4, Worksheet.MaxColumns - 1), range.End.Token.Address);
    }

    [Fact]
    public void Parser_ReversedColumnRange_IsNormalized()
    {
        var range = Assert.IsType<RangeSyntaxNode>(FormulaParser.Parse("=C:A").Root);

        Assert.Equal(0, range.Start.Token.Address.Column);
        Assert.Equal(2, range.End.Token.Address.Column);
    }

    [Fact]
    public void Parser_SheetOnStartOnly_AppliesToBothEnds()
    {
        var range = Assert.IsType<RangeSyntaxNode>(FormulaParser.Parse("=Data!A:B").Root);

        Assert.Equal("Data", range.Start.Token.Address.Worksheet);
        Assert.Equal("Data", range.End.Token.Address.Worksheet);
    }

    [Theory]
    [InlineData("=SUM(A:A)", 5, 1, "=SUM(B:B)")]
    [InlineData("=SUM($A:$A)", 5, 1, "=SUM($A:$A)")]
    [InlineData("=SUM($A:B)", 0, 1, "=SUM($A:C)")]
    [InlineData("=SUM(4:4)", 1, 3, "=SUM(5:5)")]
    [InlineData("=SUM($4:$4)", 1, 3, "=SUM($4:$4)")]
    [InlineData("=SUM(Data!A:B)", 0, 1, "=SUM(Data!B:C)")]
    [InlineData("=SUM('My Data'!4:5)", 2, 0, "=SUM('My Data'!6:7)")]
    [InlineData("=SUM(A:A)", 0, -1, "=SUM(#REF!)")]
    [InlineData("=SUM(1:1)", -1, 0, "=SUM(#REF!)")]
    public void CopyFormula_ShiftsOnlyTheBoundedAxis(string formula, int rowDelta, int colDelta, string expected)
    {
        Assert.Equal(expected, Adjust(formula, rowDelta, colDelta));
    }

    [Fact]
    public void Localizer_KeepsColumnAndRowRanges()
    {
        var german = CultureInfo.GetCultureInfo("de-DE");

        Assert.Equal("=SUM(A:A;4:4)", FormulaLocalizer.ToLocalized("=SUM(A:A,4:4)", german));
        Assert.Equal("=SUM(A:A,4:4)", FormulaLocalizer.ToInvariant("=SUM(A:A;4:4)", german));
    }
}
