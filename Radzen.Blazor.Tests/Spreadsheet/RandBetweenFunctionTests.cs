using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class RandBetweenFunctionTests
{
    readonly Sheet sheet = new(5, 5);

    [Theory]
    [InlineData(1, 100)]
    [InlineData(-1, 1)]
    [InlineData(0, 0)]
    public void RandBetween_ShouldReturnInclusiveRange(int bottom, int top)
    {
        static string Tok(int n) => n < 0 ? $"0{n}" : n.ToString();
        sheet.Cells["A1"].Formula = $"=RANDBETWEEN({Tok(bottom)},{Tok(top)})";
        var v = sheet.Cells["A1"].Value;
        Assert.IsType<double>(v); // numeric stored as double
        var d = (double)v;
        Assert.True(d >= bottom && d <= top);
    }

    [Fact]
    public void RandBetween_ShouldReturnNumError_WhenBottomGreaterThanTop()
    {
        sheet.Cells["A1"].Formula = "=RANDBETWEEN(5,1)";
        Assert.Equal(CellError.Num, sheet.Cells["A1"].Value);
    }
}


