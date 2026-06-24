using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class AbsFunctionTests
{
    readonly Worksheet sheet = new(5, 5);

    [Fact]
    public void ShouldReturnAbsoluteOfNegative()
    {
        sheet.Cells["A1"].Formula = "=ABS(-5)";
        Assert.Equal(5d, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldReturnAbsoluteOfPositive()
    {
        sheet.Cells["A1"].Formula = "=ABS(5)";
        Assert.Equal(5d, sheet.Cells["A1"].Value);
    }
}
