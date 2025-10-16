using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class SubstituteFunctionTests
{
    [Fact]
    public void Substitute_AllOccurrences()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A2"].Value = "Sales Data";
        sheet.Cells["B1"].Formula = "=SUBSTITUTE(A2, \"Sales\", \"Cost\")";
        Assert.Equal("Cost Data", sheet.Cells["B1"].Data.Value);
    }

    [Fact]
    public void Substitute_FirstInstanceOnly()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A3"].Value = "Quarter 1, 2008";
        sheet.Cells["B1"].Formula = "=SUBSTITUTE(A3, \"1\", \"2\", 1)";
        Assert.Equal("Quarter 2, 2008", sheet.Cells["B1"].Data.Value);
    }

    [Fact]
    public void Substitute_ThirdInstanceOnly()
    {
        var sheet = new Sheet(10, 20);
        sheet.Cells["A4"].Value = "Quarter 1, 2011";
        sheet.Cells["B1"].Formula = "=SUBSTITUTE(A4, \"1\", \"2\", 3)";
        Assert.Equal("Quarter 1, 2012", sheet.Cells["B1"].Data.Value);
    }
}