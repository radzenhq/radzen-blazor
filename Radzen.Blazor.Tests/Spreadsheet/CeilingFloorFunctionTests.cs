using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class CeilingFloorFunctionTests
{
    readonly Worksheet sheet = new(5, 5);

    [Fact]
    public void ShouldCeilingAwayFromZero()
    {
        sheet.Cells["A1"].Formula = "=CEILING(2.5,1)";
        Assert.Equal(3d, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldCeilingToMultiple()
    {
        sheet.Cells["A1"].Formula = "=CEILING(2.5,2)";
        Assert.Equal(4d, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldFloorDownToMultiple()
    {
        sheet.Cells["A1"].Formula = "=FLOOR(2.5,2)";
        Assert.Equal(2d, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldHandleZeroSignificance()
    {
        // Verified against Excel: CEILING returns 0, FLOOR returns #DIV/0! (asymmetric).
        sheet.Cells["A1"].Formula = "=CEILING(5,0)";
        Assert.Equal(0d, sheet.Cells["A1"].Value);

        sheet.Cells["A2"].Formula = "=FLOOR(5,0)";
        Assert.Equal(CellError.Div0, sheet.Cells["A2"].Value);
    }

    [Fact]
    public void ShouldReturnNumOnSignMismatch()
    {
        sheet.Cells["A1"].Formula = "=CEILING(5,-2)";
        Assert.Equal(CellError.Num, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldFloorNegativeAwayFromZero()
    {
        sheet.Cells["A1"].Formula = "=FLOOR(-2.5,2)";
        Assert.Equal(-4d, sheet.Cells["A1"].Value);
    }
}
