using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class LookupBlankValueTests
{
    readonly Worksheet sheet = new(20, 20);

    public LookupBlankValueTests()
    {
        sheet.Cells["B1"].Value = "a";
        sheet.Cells["B3"].Value = "c";
        sheet.Cells["C1"].Value = 10;
        sheet.Cells["C2"].Value = 20;
        sheet.Cells["C3"].Value = 30;
        sheet.Cells["E1"].Value = "a";
        sheet.Cells["E2"].Value = 0;
        sheet.Cells["E3"].Value = "c";
        sheet.Cells["F1"].Value = 10;
        sheet.Cells["F2"].Value = 20;
        sheet.Cells["F3"].Value = 30;
        sheet.Cells["H1"].Value = "a";
        sheet.Cells["H2"].Formula = "=\"\"";
        sheet.Cells["H3"].Value = "c";
        sheet.Cells["I1"].Value = 10;
        sheet.Cells["I2"].Value = 20;
        sheet.Cells["I3"].Value = 30;
    }

    private object? Evaluate(string formula)
    {
        sheet.Cells["M10"].Formula = formula;
        return sheet.Cells["M10"].Value;
    }

    [Theory]
    [InlineData("=VLOOKUP(A1,E1:F3,2,FALSE)", 20d)]
    [InlineData("=VLOOKUP(A1,E1:F3,2,TRUE)", 20d)]
    [InlineData("=MATCH(A1,E1:E3,0)", 2d)]
    [InlineData("=VLOOKUP(\"\",H1:I3,2,FALSE)", 20d)]
    [InlineData("=XLOOKUP(A1,B1:B3,C1:C3)", 20d)]
    [InlineData("=XMATCH(A1,B1:B3)", 2d)]
    public void BlankLookupValueMatchesAsInExcel(string formula, double expected)
    {
        Assert.Equal(expected, Evaluate(formula));
    }

    [Theory]
    [InlineData("=VLOOKUP(A1,B1:C3,2,FALSE)")]
    [InlineData("=VLOOKUP(A1,H1:I3,2,FALSE)")]
    [InlineData("=VLOOKUP(\"\",B1:C3,2,FALSE)")]
    [InlineData("=MATCH(A1,B1:B3,0)")]
    [InlineData("=XLOOKUP(A1,E1:E3,F1:F3)")]
    [InlineData("=HLOOKUP(A1,B1:B3,1,FALSE)")]
    public void BlankLookupValueDoesNotMatchAsInExcel(string formula)
    {
        Assert.Equal(CellError.NA, Evaluate(formula));
    }

    [Fact]
    public void ApproximateLookupOfANumberSkipsTextInsteadOfFailing()
    {
        Assert.Equal(20d, Evaluate("=VLOOKUP(5,E1:F3,2,TRUE)"));
        Assert.Equal(2d, Evaluate("=MATCH(5,E1:E3,1)"));
    }
}
