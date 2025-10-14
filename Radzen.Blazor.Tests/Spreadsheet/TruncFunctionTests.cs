using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class TruncFunctionTests
{
    readonly Sheet sheet = new(10, 10);

    [Fact]
    public void ShouldTruncatePositive()
    {
        sheet.Cells["A1"].Formula = "=TRUNC(8.9)";
        Assert.Equal(8d, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldTruncateNegative()
    {
        sheet.Cells["A1"].Formula = "=TRUNC(0-8.9)";
        Assert.Equal(-8d, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldTruncateLessThanOne()
    {
        sheet.Cells["A1"].Formula = "=TRUNC(0.45)";
        Assert.Equal(0d, sheet.Cells["A1"].Value);
    }
}


