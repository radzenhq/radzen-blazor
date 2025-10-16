using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class UpperFunctionTests
{
    [Fact]
    public void Upper_ConvertsToUppercase()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A2"].Value = "total";
        sheet.Cells["B1"].Formula = "=UPPER(A2)";
        Assert.Equal("TOTAL", sheet.Cells["B1"].Data.Value);
    }

    [Fact]
    public void Upper_AlreadyUppercase()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A3"].Value = "Yield";
        sheet.Cells["B1"].Formula = "=UPPER(A3)";
        Assert.Equal("YIELD", sheet.Cells["B1"].Data.Value);
    }
}