using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class VerticalLookupFunctionTests
{
    readonly Sheet sheet = new(10, 10);

    [Fact]
    public void ShouldFindExactMatchInTwoColumnRange()
    {
        sheet.Cells["A1"].Value = "T-Shirt";
        sheet.Cells["A2"].Value = "Jeans";
        sheet.Cells["B1"].Value = 19.99;
        sheet.Cells["B2"].Value = 29.99;

        sheet.Cells["C1"].Formula = "=VLOOKUP(\"T-Shirt\",A1:B2,2,0)";

        Assert.Equal(19.99, sheet.Cells["C1"].Value);
    }

    [Fact]
    public void ShouldReturnNAWhenNoExactMatch()
    {
        sheet.Cells["A1"].Value = "Hat";
        sheet.Cells["B1"].Value = 9.99;

        sheet.Cells["C1"].Formula = "=VLOOKUP(\"Gloves\",A1:B1,2,0)";

        Assert.Equal(CellError.NA, sheet.Cells["C1"].Value);
    }

    [Fact]
    public void ShouldFindApproximateMatchInSortedFirstColumn()
    {
        // First column sorted ascending
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["A2"].Value = 20;
        sheet.Cells["A3"].Value = 30;

        sheet.Cells["B1"].Value = "Low";
        sheet.Cells["B2"].Value = "Medium";
        sheet.Cells["B3"].Value = "High";

        // search_key 25 -> should pick row with 20
        sheet.Cells["C1"].Formula = "=VLOOKUP(25,A1:B3,2,1)";

        Assert.Equal("Medium", sheet.Cells["C1"].Value);
    }

    [Fact]
    public void ShouldErrorWhenIndexOutOfRange()
    {
        sheet.Cells["A1"].Value = "X";
        sheet.Cells["B1"].Value = 1;

        sheet.Cells["C1"].Formula = "=VLOOKUP(\"X\",A1:B1,3,0)";

        Assert.Equal(CellError.Ref, sheet.Cells["C1"].Value);
    }
}