using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class LookupFunctionTests
{
    readonly Worksheet sheet = new(30, 20);

    public LookupFunctionTests()
    {
        sheet.Cells["A1"].Value = 1;
        sheet.Cells["A2"].Value = 5;
        sheet.Cells["A3"].Value = 10;
        sheet.Cells["B1"].Value = "one";
        sheet.Cells["B2"].Value = "five";
        sheet.Cells["B3"].Value = "ten";
        sheet.Cells["B4"].Value = "beyond";
        sheet.Cells["D1"].Value = "apple";
        sheet.Cells["D2"].Value = "Banana";
        sheet.Cells["D3"].Value = "cherry";
        sheet.Cells["E1"].Value = 1;
        sheet.Cells["E2"].Value = "x";
        sheet.Cells["E3"].Value = 7;
        sheet.Cells["E4"].Value = 9;
        sheet.Cells["F1"].Value = "x";
        sheet.Cells["F2"].Value = "y";
        sheet.Cells["F3"].Value = "x";
        sheet.Cells["F4"].Value = "y";
        sheet.Cells["G1"].Value = 10;
        sheet.Cells["G2"].Value = 20;
        sheet.Cells["G3"].Value = 30;
        sheet.Cells["G4"].Value = 40;
        sheet.Cells["H1"].Value = 1;
        sheet.Cells["I1"].Value = 2;
        sheet.Cells["J1"].Value = 3;
        sheet.Cells["H2"].Value = "a";
        sheet.Cells["I2"].Value = "b";
        sheet.Cells["J2"].Value = "c";
        sheet.Cells["K1"].Value = 5;
        sheet.Cells["K2"].Value = 3;
        sheet.Cells["K3"].Value = 8;
    }

    private object? Evaluate(string formula)
    {
        sheet.Cells["M20"].Formula = formula;
        return sheet.Cells["M20"].Value;
    }

    [Theory]
    [InlineData("=LOOKUP(5,A1:A3,B1:B3)", "five")]
    [InlineData("=LOOKUP(7,A1:A3,B1:B3)", "five")]
    [InlineData("=LOOKUP(100,A1:A3,B1:B3)", "ten")]
    [InlineData("=LOOKUP(6,K1:K3,B1:B3)", "five")]
    public void VectorFormFindsTheLargestValueNotGreaterThanTheLookupValue(string formula, string expected)
    {
        Assert.Equal(expected, Evaluate(formula));
    }

    [Fact]
    public void VectorFormReturnsNAWhenTheLookupValueIsBelowEveryValue()
    {
        Assert.Equal(CellError.NA, Evaluate("=LOOKUP(0,A1:A3,B1:B3)"));
    }

    [Fact]
    public void TextIsComparedCaseInsensitively()
    {
        Assert.Equal(5d, Evaluate("=LOOKUP(\"BANANA\",D1:D3,A1:A3)"));
        Assert.Equal(1d, Evaluate("=LOOKUP(\"b\",D1:D3,A1:A3)"));
    }

    [Fact]
    public void ValuesOfAnotherTypeAreSkipped()
    {
        Assert.Equal(30d, Evaluate("=LOOKUP(8,E1:E4,G1:G4)"));
        Assert.Equal(CellError.NA, Evaluate("=LOOKUP(\"z\",A1:A3,B1:B3)"));
    }

    [Fact]
    public void ErrorsInTheLookupVectorAreSkippedSoTheLastMatchIsFound()
    {
        Assert.Equal(30d, Evaluate("=LOOKUP(2,1/(F1:F4=\"x\"),G1:G4)"));
    }

    [Fact]
    public void HorizontalVectorsReturnFromTheSamePosition()
    {
        Assert.Equal("b", Evaluate("=LOOKUP(2,H1:J1,H2:J2)"));
    }

    [Fact]
    public void ResultVectorOfAnotherOrientationIsIndexedByPosition()
    {
        Assert.Equal("c", Evaluate("=LOOKUP(10,A1:A3,H2:J2)"));
    }

    [Fact]
    public void ResultVectorShorterThanTheLookupVectorExtendsDownTheSheet()
    {
        Assert.Equal("ten", Evaluate("=LOOKUP(10,A1:A3,B1:B2)"));
    }

    [Fact]
    public void ArrayFormWiderThanTallSearchesTheFirstRowAndReturnsFromTheLastRow()
    {
        Assert.Equal("b", Evaluate("=LOOKUP(2,H1:J2)"));
    }

    [Fact]
    public void ArrayFormTallerThanWideSearchesTheFirstColumnAndReturnsFromTheLastColumn()
    {
        Assert.Equal("five", Evaluate("=LOOKUP(7,A1:B3)"));
    }

    [Fact]
    public void ErrorLookupValueIsReturned()
    {
        Assert.Equal(CellError.Div0, Evaluate("=LOOKUP(1/0,A1:A3,B1:B3)"));
    }
}
