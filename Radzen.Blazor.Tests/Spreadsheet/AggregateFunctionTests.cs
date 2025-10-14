using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class AggregateFunctionTests
{
    readonly Sheet sheet = new(15, 5);

    void SeedWithErrors()
    {
        sheet.Cells["A1"].Formula = "=A2/0"; // #DIV/0!
        sheet.Cells["A2"].Value = 82;
        sheet.Cells["A3"].Value = 72;
        sheet.Cells["A4"].Value = 65;
        sheet.Cells["A5"].Value = 30;
        sheet.Cells["A6"].Value = 95;
        sheet.Cells["A7"].Formula = "=0/0"; // #DIV/0!
        sheet.Cells["A8"].Value = 63;
        sheet.Cells["A9"].Value = 31;
        sheet.Cells["A10"].Value = 53;
        sheet.Cells["A11"].Value = 96;
    }

    [Fact]
    public void ShouldComputeMaxIgnoringErrors()
    {
        SeedWithErrors();
        sheet.Cells["B1"].Formula = "=AGGREGATE(4,6,A1:A11)"; // MAX ignoring errors
        Assert.Equal(96d, sheet.Cells["B1"].Value);
    }

    [Fact]
    public void ShouldComputeLargeIgnoringErrors()
    {
        SeedWithErrors();
        sheet.Cells["B1"].Formula = "=AGGREGATE(14,6,A1:A11,3)"; // LARGE k=3 ignoring errors
        Assert.Equal(82d, sheet.Cells["B1"].Value);
    }

    [Fact]
    public void ShouldReturnValueErrorWhenKMissingForSmall()
    {
        SeedWithErrors();
        sheet.Cells["B1"].Formula = "=AGGREGATE(15,6,A1:A11)"; // SMALL requires k
        Assert.Equal(CellError.Value, sheet.Cells["B1"].Value);
    }
}


