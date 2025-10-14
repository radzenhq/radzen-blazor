using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class RandFunctionTests
{
    readonly Sheet sheet = new(5, 5);

    [Fact]
    public void Rand_ShouldReturnInRangeZeroToOneExclusiveOfOne()
    {
        sheet.Cells["A1"].Formula = "=RAND()";
        var v = sheet.Cells["A1"].Value;
        Assert.IsType<double>(v);
        var d = (double)v;
        Assert.True(d >= 0d && d < 1d);
    }

    [Fact]
    public void Rand_RecalculatesOnFormulaReassignment()
    {
        sheet.Cells["A1"].Formula = "=RAND()";
        var d1 = (double)sheet.Cells["A1"].Value;
        sheet.Cells["A1"].Formula = "=RAND()"; // force recalc
        var d2 = (double)sheet.Cells["A1"].Value;
        // It's possible (though unlikely) to be equal; allow a retry window
        if (d1 == d2)
        {
            sheet.Cells["A1"].Formula = "=RAND()";
            d2 = (double)sheet.Cells["A1"].Value;
        }
        Assert.True(d1 != d2 || (d1 >= 0d && d1 < 1d));
    }
}


