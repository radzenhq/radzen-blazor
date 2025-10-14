using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class LargeFunctionTests
{
    readonly Sheet sheet = new(10, 10);

    [Fact]
    public void ShouldReturnKthLargestAcrossRange()
    {
        // Populate A2:B6 as in example
        sheet.Cells["A2"].Value = 3;
        sheet.Cells["A3"].Value = 4;
        sheet.Cells["A4"].Value = 5;
        sheet.Cells["A5"].Value = 2;
        sheet.Cells["A6"].Value = 3;

        sheet.Cells["B2"].Value = 4;
        sheet.Cells["B3"].Value = 5;
        sheet.Cells["B4"].Value = 6;
        sheet.Cells["B5"].Value = 4;
        sheet.Cells["B6"].Value = 7;

        sheet.Cells["C1"].Formula = "=LARGE(A2:B6,3)";

        Assert.Equal(5d, sheet.Cells["C1"].Value);
    }

    [Fact]
    public void ShouldReturn7thLargestAsFour()
    {
        sheet.Cells["A2"].Value = 3;
        sheet.Cells["A3"].Value = 4;
        sheet.Cells["A4"].Value = 5;
        sheet.Cells["A5"].Value = 2;
        sheet.Cells["A6"].Value = 3;

        sheet.Cells["B2"].Value = 4;
        sheet.Cells["B3"].Value = 5;
        sheet.Cells["B4"].Value = 6;
        sheet.Cells["B5"].Value = 4;
        sheet.Cells["B6"].Value = 7;

        sheet.Cells["C1"].Formula = "=LARGE(A2:B6,7)";

        Assert.Equal(4d, sheet.Cells["C1"].Value);
    }

    [Fact]
    public void ShouldReturnNumErrorForInvalidK()
    {
        sheet.Cells["A1"].Value = 1;
        sheet.Cells["A2"].Value = 2;
        sheet.Cells["A3"].Value = 3;

        sheet.Cells["B1"].Formula = "=LARGE(A1:A3,0)";
        Assert.Equal(CellError.Num, sheet.Cells["B1"].Value);

        sheet.Cells["B2"].Formula = "=LARGE(A1:A3,5)";
        Assert.Equal(CellError.Num, sheet.Cells["B2"].Value);
    }

    [Fact]
    public void ShouldIgnoreNonNumericCellsInArray()
    {
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["A2"].Value = "x"; // ignored
        sheet.Cells["A3"].Value = 7;

        sheet.Cells["B1"].Formula = "=LARGE(A1:A3,2)";
        Assert.Equal(7d, sheet.Cells["B1"].Value);
    }
}