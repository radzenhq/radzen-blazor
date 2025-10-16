using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class LowerFunctionTests
{
    [Fact]
    public void Lower_ConvertsToLowercase()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A2"].Value = "E. E. Cummings";
        sheet.Cells["B1"].Formula = "=LOWER(A2)";
        Assert.Equal("e. e. cummings", sheet.Cells["B1"].Data.Value);
    }

    [Fact]
    public void Lower_IgnoresNonLetters()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A3"].Value = "Apt. 2B";
        sheet.Cells["B1"].Formula = "=LOWER(A3)";
        Assert.Equal("apt. 2b", sheet.Cells["B1"].Data.Value);
    }
}