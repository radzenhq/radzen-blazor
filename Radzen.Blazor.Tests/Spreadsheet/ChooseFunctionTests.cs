using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class ChooseFunctionTests
{
    readonly Sheet sheet = new(10, 10);

    [Fact]
    public void ShouldPickScalarByIndex()
    {
        sheet.Cells["A1"].Formula = "=CHOOSE(3,\"Wide\",115,\"world\",8)";
        Assert.Equal("world", sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldPickCellReferenceByIndex()
    {
        sheet.Cells["A2"].Value = "1st";
        sheet.Cells["A3"].Value = "2nd";
        sheet.Cells["A4"].Value = "3rd";
        sheet.Cells["A5"].Value = "Finished";

        sheet.Cells["B1"].Formula = "=CHOOSE(2,A2,A3,A4,A5)";
        Assert.Equal("2nd", sheet.Cells["B1"].Value);
    }

    [Fact]
    public void ShouldReturnValueErrorWhenIndexOutOfRange()
    {
        sheet.Cells["A1"].Formula = "=CHOOSE(5,1,2,3)";
        Assert.Equal(CellError.Value, sheet.Cells["A1"].Value);
    }
}