using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class IntFunctionTests
{
    readonly Sheet sheet = new(10, 10);

    [Fact]
    public void ShouldRoundDownPositive()
    {
        sheet.Cells["A1"].Formula = "=INT(8.9)";
        Assert.Equal(8d, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldRoundDownNegative()
    {
        sheet.Cells["A1"].Formula = "=INT(0-8.9)";
        Assert.Equal(-9d, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldReturnDecimalPart()
    {
        sheet.Cells["A2"].Value = 19.5;
        sheet.Cells["A1"].Formula = "=A2-INT(A2)";
        Assert.Equal(0.5, sheet.Cells["A1"].Value);
    }
}