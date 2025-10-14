using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class RoundFunctionTests
{
    readonly Sheet sheet = new(10, 10);

    [Fact]
    public void ShouldRoundToOneDecimalPlace()
    {
        sheet.Cells["A1"].Formula = "=ROUND(2.15,1)";
        Assert.Equal(2.2, sheet.Cells["A1"].Value);

        sheet.Cells["A2"].Formula = "=ROUND(2.149,1)";
        Assert.Equal(2.1, sheet.Cells["A2"].Value);
    }

    [Fact]
    public void ShouldRoundNegativeWithAwayFromZeroMidpoint()
    {
        sheet.Cells["A1"].Formula = "=ROUND(0-1.475,2)";
        Assert.Equal(-1.48, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldRoundWithNegativeDigits()
    {
        sheet.Cells["A1"].Formula = "=ROUND(21.5,0-1)";
        Assert.Equal(20d, sheet.Cells["A1"].Value);

        sheet.Cells["A2"].Formula = "=ROUND(626.3,0-3)";
        Assert.Equal(1000d, sheet.Cells["A2"].Value);

        sheet.Cells["A3"].Formula = "=ROUND(1.98,0-1)";
        Assert.Equal(0d, sheet.Cells["A3"].Value);

        sheet.Cells["A4"].Formula = "=ROUND(0-50.55,0-2)";
        Assert.Equal(-100d, sheet.Cells["A4"].Value);
    }
}