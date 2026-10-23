using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class XLookupTypeOrderTests
{
    readonly Worksheet sheet = new(20, 30);

    public XLookupTypeOrderTests()
    {
        sheet.Cells["B1"].Value = 1;
        sheet.Cells["B2"].Value = "x";
        sheet.Cells["B3"].Value = false;
        sheet.Cells["B4"].Value = true;
        sheet.Cells["B6"].Formula = "=\"\"";
        sheet.Cells["B7"].Value = 5;

        sheet.Cells["E1"].Value = "a";
        sheet.Cells["E2"].Value = 0;
        sheet.Cells["E3"].Value = "c";
        sheet.Cells["E4"].Value = 5;

        sheet.Cells["H1"].Value = "a";
        sheet.Cells["H2"].Formula = "=\"\"";
        sheet.Cells["H3"].Value = "c";
        sheet.Cells["H4"].Value = 5;

        sheet.Cells["K1"].Value = "a";
        sheet.Cells["K3"].Value = "c";
        sheet.Cells["K4"].Value = 5;

        sheet.Cells["N1"].Value = 1;
        sheet.Cells["N2"].Value = 3;
        sheet.Cells["N3"].Value = 5;
        sheet.Cells["N4"].Formula = "=\"\"";
        sheet.Cells["N5"].Value = "a";
        sheet.Cells["N6"].Value = "m";
        sheet.Cells["N7"].Value = false;
        sheet.Cells["N8"].Value = true;

        for (var row = 1; row <= 8; row++)
        {
            sheet.Cells[$"C{row}"].Value = row * 10;
            sheet.Cells[$"I{row}"].Value = row * 10;
            sheet.Cells[$"L{row}"].Value = row * 10;
            sheet.Cells[$"O{row}"].Value = row * 10;
        }
    }

    private object? Evaluate(string formula)
    {
        sheet.Cells["Z1"].Formula = formula;
        return sheet.Cells["Z1"].Value;
    }

    [Theory]
    [InlineData("=XLOOKUP(A1,H1:H4,I1:I4,\"nf\")")]
    [InlineData("=XLOOKUP(\"\",K1:K4,L1:L4,\"nf\")")]
    public void XLookupDoesNotMatchABlankWithAnEmptyString(string formula)
    {
        Assert.Equal("nf", Evaluate(formula));
    }

    [Theory]
    [InlineData("=XMATCH(6,B1:B7,1)", 6d)]
    [InlineData("=XMATCH(6,B1:B7,-1)", 7d)]
    [InlineData("=XMATCH(\"a\",B1:B7,-1)", 6d)]
    [InlineData("=XMATCH(\"a\",B1:B7,1)", 2d)]
    [InlineData("=XMATCH(\"z\",B1:B7,1)", 3d)]
    [InlineData("=XMATCH(A1,E1:E4,-1)", 3d)]
    [InlineData("=XMATCH(FALSE,E1:E4,-1)", 3d)]
    [InlineData("=XMATCH(\"\",E1:E4,-1)", 4d)]
    public void XMatchOrdersNumbersThenTextThenLogicalValuesThenBlank(string formula, double expected)
    {
        Assert.Equal(expected, Evaluate(formula));
    }

    [Fact]
    public void XMatchFindsNothingSmallerThanTheSmallestNumber()
    {
        Assert.Equal(CellError.NA, Evaluate("=XMATCH(0,B1:B7,-1)"));
    }

    [Theory]
    [InlineData("=XLOOKUP(3,B1:B7,C1:C7,\"nf\",1)", 70d)]
    [InlineData("=XLOOKUP(6,B1:B7,C1:C7,\"nf\",1)", 60d)]
    [InlineData("=XLOOKUP(\"z\",B1:B7,C1:C7,\"nf\",1)", 30d)]
    public void XLookupNextLargerReturnsTheSmallestLargerValueInTypeOrder(string formula, double expected)
    {
        Assert.Equal(expected, Evaluate(formula));
    }

    [Theory]
    [InlineData("=XLOOKUP(4,N1:N8,O1:O8,\"nf\",-1,2)", 20d)]
    [InlineData("=XLOOKUP(4,N1:N8,O1:O8,\"nf\",1,2)", 30d)]
    [InlineData("=XLOOKUP(\"b\",N1:N8,O1:O8,\"nf\",-1,2)", 50d)]
    [InlineData("=XLOOKUP(\"b\",N1:N8,O1:O8,\"nf\",1,2)", 60d)]
    [InlineData("=XLOOKUP(FALSE,N1:N8,O1:O8,\"nf\",0,2)", 70d)]
    public void XLookupBinarySearchUsesTheSameTypeOrder(string formula, double expected)
    {
        Assert.Equal(expected, Evaluate(formula));
    }
}
