using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class StatisticalFunctionTests
{
    readonly Worksheet sheet = new(10, 10);

    [Fact]
    public void ShouldComputeSampleStdev()
    {
        sheet.Cells["A1"].Formula = "=STDEV(2,4,6)";
        Assert.Equal(2d, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldComputePopulationStdev()
    {
        sheet.Cells["A1"].Formula = "=STDEVP(2,4)";
        Assert.Equal(1d, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldComputeSampleVar()
    {
        sheet.Cells["A1"].Formula = "=VAR(2,4,6)";
        Assert.Equal(4d, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldComputePopulationVar()
    {
        sheet.Cells["A1"].Formula = "=VARP(2,4)";
        Assert.Equal(1d, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldReturnDiv0ForSampleStdevWithOneValue()
    {
        sheet.Cells["A1"].Formula = "=STDEV(5)";
        Assert.Equal(CellError.Div0, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldComputeMode()
    {
        sheet.Cells["A1"].Formula = "=MODE(1,2,2,3)";
        Assert.Equal(2d, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldReturnNAForModeWhenAllUnique()
    {
        sheet.Cells["A1"].Formula = "=MODE(1,2,3)";
        Assert.Equal(CellError.NA, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldSupportDottedAliases()
    {
        sheet.Cells["A1"].Formula = "=STDEV.S(2,4,6)";
        Assert.Equal(2d, sheet.Cells["A1"].Value);

        sheet.Cells["A2"].Formula = "=VAR.P(2,4)";
        Assert.Equal(1d, sheet.Cells["A2"].Value);

        sheet.Cells["A3"].Formula = "=MODE.SNGL(1,2,2,3)";
        Assert.Equal(2d, sheet.Cells["A3"].Value);
    }
}
