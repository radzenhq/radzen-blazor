using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class ModFunctionTests
{
    readonly Worksheet sheet = new(5, 5);

    [Fact]
    public void ShouldComputeSimpleMod()
    {
        sheet.Cells["A1"].Formula = "=MOD(10,3)";
        Assert.Equal(1d, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldTakeSignOfDivisor()
    {
        sheet.Cells["A1"].Formula = "=MOD(-10,3)";
        Assert.Equal(2d, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldReturnDiv0ForZeroDivisor()
    {
        sheet.Cells["A1"].Formula = "=MOD(10,0)";
        Assert.Equal(CellError.Div0, sheet.Cells["A1"].Value);
    }
}
