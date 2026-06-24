using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class RankFunctionTests
{
    readonly Worksheet sheet = new(10, 10);

    void Seed()
    {
        sheet.Cells["A1"].Value = 1;
        sheet.Cells["A2"].Value = 5;
        sheet.Cells["A3"].Value = 10;
    }

    [Fact]
    public void ShouldRankDescendingByDefault()
    {
        Seed();
        sheet.Cells["B1"].Formula = "=RANK(5,A1:A3)";
        Assert.Equal(2d, sheet.Cells["B1"].Value);
    }

    [Fact]
    public void ShouldRankAscendingWhenOrderNonZero()
    {
        Seed();
        sheet.Cells["B1"].Formula = "=RANK(5,A1:A3,1)";
        Assert.Equal(2d, sheet.Cells["B1"].Value);
    }

    [Fact]
    public void ShouldReturnNAWhenNotInReference()
    {
        Seed();
        sheet.Cells["B1"].Formula = "=RANK(7,A1:A3)";
        Assert.Equal(CellError.NA, sheet.Cells["B1"].Value);
    }

    [Fact]
    public void ShouldSupportRankEqAlias()
    {
        Seed();
        sheet.Cells["B1"].Formula = "=RANK.EQ(10,A1:A3)";
        Assert.Equal(1d, sheet.Cells["B1"].Value);
    }
}
