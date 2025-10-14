using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class SmallFunctionTests
{
    readonly Sheet sheet = new(12, 12);

    [Fact]
    public void ShouldReturn4thSmallestInFirstColumn()
    {
        sheet.Cells["A2"].Value = 3;
        sheet.Cells["A3"].Value = 4;
        sheet.Cells["A4"].Value = 5;
        sheet.Cells["A5"].Value = 2;
        sheet.Cells["A6"].Value = 3;
        sheet.Cells["A7"].Value = 4;
        sheet.Cells["A8"].Value = 6;
        sheet.Cells["A9"].Value = 4;
        sheet.Cells["A10"].Value = 7;

        sheet.Cells["B1"].Formula = "=SMALL(A2:A10,4)";

        Assert.Equal(4d, sheet.Cells["B1"].Value);
    }

    [Fact]
    public void ShouldReturn2ndSmallestInSecondColumn()
    {
        sheet.Cells["B2"].Value = 1;
        sheet.Cells["B3"].Value = 4;
        sheet.Cells["B4"].Value = 8;
        sheet.Cells["B5"].Value = 3;
        sheet.Cells["B6"].Value = 7;
        sheet.Cells["B7"].Value = 12;
        sheet.Cells["B8"].Value = 54;
        sheet.Cells["B9"].Value = 8;
        sheet.Cells["B10"].Value = 23;

        sheet.Cells["C1"].Formula = "=SMALL(B2:B10,2)";

        Assert.Equal(3d, sheet.Cells["C1"].Value);
    }

    [Fact]
    public void ShouldErrorForEmptyArrayOrInvalidK()
    {
        sheet.Cells["A1"].Formula = "=SMALL(A2:A2,1)"; // empty range
        Assert.Equal(CellError.Num, sheet.Cells["A1"].Value);

        sheet.Cells["A2"].Value = 5;
        sheet.Cells["A3"].Formula = "=SMALL(A2:A2,0)"; // k <= 0
        Assert.Equal(CellError.Num, sheet.Cells["A3"].Value);

        sheet.Cells["A4"].Formula = "=SMALL(A2:A2,2)"; // k > count
        Assert.Equal(CellError.Num, sheet.Cells["A4"].Value);
    }
}
