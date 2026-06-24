using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class PowerSqrtFunctionTests
{
    readonly Worksheet sheet = new(5, 5);

    [Fact]
    public void ShouldComputePower()
    {
        sheet.Cells["A1"].Formula = "=POWER(2,10)";
        Assert.Equal(1024d, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldReturnNumForNaNPower()
    {
        sheet.Cells["A1"].Formula = "=POWER(-1,0.5)";
        Assert.Equal(CellError.Num, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldComputeSqrt()
    {
        sheet.Cells["A1"].Formula = "=SQRT(16)";
        Assert.Equal(4d, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldReturnNumForNegativeSqrt()
    {
        sheet.Cells["A1"].Formula = "=SQRT(-4)";
        Assert.Equal(CellError.Num, sheet.Cells["A1"].Value);
    }
}
