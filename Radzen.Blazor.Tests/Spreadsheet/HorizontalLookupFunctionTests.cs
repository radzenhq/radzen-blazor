using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class HorizontalLookupFunctionTests
{
    readonly Sheet sheet = new(10, 10);

    [Fact]
    public void ShouldFindExactMatchInTwoRowRange()
    {
        sheet.Cells["A1"].Value = "Size";
        sheet.Cells["B1"].Value = "Color";
        sheet.Cells["A2"].Value = "M";
        sheet.Cells["B2"].Value = "Blue";

        sheet.Cells["C1"].Formula = "=HLOOKUP(\"Color\",A1:B2,2,0)";

        Assert.Equal("Blue", sheet.Cells["C1"].Value);
    }

    [Fact]
    public void ShouldReturnNAWhenNoExactMatch()
    {
        sheet.Cells["A1"].Value = "Size";
        sheet.Cells["A2"].Value = "M";

        sheet.Cells["B1"].Formula = "=HLOOKUP(\"Color\",A1:A2,2,0)";

        Assert.Equal(CellError.NA, sheet.Cells["B1"].Value);
    }

    [Fact]
    public void ShouldFindApproximateMatchInSortedTopRow()
    {
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["B1"].Value = 20;
        sheet.Cells["C1"].Value = 30;

        sheet.Cells["A2"].Value = "Low";
        sheet.Cells["B2"].Value = "Medium";
        sheet.Cells["C2"].Value = "High";

        sheet.Cells["D1"].Formula = "=HLOOKUP(25,A1:C2,2,1)";

        Assert.Equal("Medium", sheet.Cells["D1"].Value);
    }

    [Fact]
    public void ShouldErrorWhenIndexOutOfRange()
    {
        sheet.Cells["A1"].Value = "X";
        sheet.Cells["A2"].Value = 1;

        sheet.Cells["B1"].Formula = "=HLOOKUP(\"X\",A1:A2,3,0)";

        Assert.Equal(CellError.Ref, sheet.Cells["B1"].Value);
    }
}