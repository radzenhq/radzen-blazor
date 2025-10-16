using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class ProperFunctionTests
{
    [Fact]
    public void Proper_TitleCase_Simple()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A2"].Value = "this is a TITLE";
        sheet.Cells["B1"].Formula = "=PROPER(A2)";
        Assert.Equal("This Is A Title", sheet.Cells["B1"].Data.Value);
    }

    [Fact]
    public void Proper_KeepsHyphenation()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A3"].Value = "2-way street";
        sheet.Cells["B1"].Formula = "=PROPER(A3)";
        Assert.Equal("2-Way Street", sheet.Cells["B1"].Data.Value);
    }

    [Fact]
    public void Proper_AlnumBoundary()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A4"].Value = "76BudGet";
        sheet.Cells["B1"].Formula = "=PROPER(A4)";
        Assert.Equal("76Budget", sheet.Cells["B1"].Data.Value);
    }
}