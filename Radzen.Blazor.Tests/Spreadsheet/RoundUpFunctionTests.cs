using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class RoundUpFunctionTests
{
    readonly Sheet sheet = new(10, 10);

    [Fact]
    public void ShouldRoundUpToZeroDecimalPlaces()
    {
        sheet.Cells["A1"].Formula = "=ROUNDUP(3.2,0)";
        Assert.Equal(4d, sheet.Cells["A1"].Value);

        sheet.Cells["A2"].Formula = "=ROUNDUP(76.9,0)";
        Assert.Equal(77d, sheet.Cells["A2"].Value);
    }

    [Fact]
    public void ShouldRoundUpToSpecifiedDecimalPlaces()
    {
        sheet.Cells["A1"].Formula = "=ROUNDUP(3.14159,3)";
        Assert.Equal(3.142, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldRoundUpNegativeNumbersAwayFromZero()
    {
        sheet.Cells["A1"].Formula = "=ROUNDUP(0-3.14159,1)";
        Assert.Equal(-3.2, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldRoundUpToLeftOfDecimalWhenNegativeDigits()
    {
        sheet.Cells["A1"].Formula = "=ROUNDUP(31415.92654,0-2)";
        Assert.Equal(31500d, sheet.Cells["A1"].Value);
    }
}