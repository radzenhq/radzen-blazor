using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class RoundDownFunctionTests
{
    readonly Sheet sheet = new(10, 10);

    [Fact]
    public void ShouldRoundDownToZeroDecimalPlaces()
    {
        sheet.Cells["A1"].Formula = "=ROUNDDOWN(3.2,0)";
        Assert.Equal(3d, sheet.Cells["A1"].Value);

        sheet.Cells["A2"].Formula = "=ROUNDDOWN(76.9,0)";
        Assert.Equal(76d, sheet.Cells["A2"].Value);
    }

    [Fact]
    public void ShouldRoundDownToSpecifiedDecimalPlaces()
    {
        sheet.Cells["A1"].Formula = "=ROUNDDOWN(3.14159,3)";
        Assert.Equal(3.141, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldRoundDownNegativeNumbersTowardZero()
    {
        sheet.Cells["A1"].Formula = "=ROUNDDOWN(0-3.14159,1)";
        Assert.Equal(-3.1, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldRoundDownToLeftOfDecimalWhenNegativeDigits()
    {
        sheet.Cells["A1"].Formula = "=ROUNDDOWN(31415.92654,0-2)";
        Assert.Equal(31400d, sheet.Cells["A1"].Value);
    }
}