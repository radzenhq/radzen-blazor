using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class TrimFunctionTests
{
    [Fact]
    public void Trim_RemovesLeadingTrailingAndCollapsesInternalSpaces()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A1"].Formula = "=TRIM(\" First  Quarter   Earnings \")";
        Assert.Equal("First Quarter Earnings", sheet.Cells["A1"].Data.Value);
    }

    [Fact]
    public void Trim_Empty_ReturnsEmpty()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A1"].Formula = "=TRIM(\"   \")";
        Assert.Equal(string.Empty, sheet.Cells["A1"].Data.Value);
    }

    [Fact]
    public void Trim_TextCell_Works()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["B1"].Value = "  Hello   world  ";
        sheet.Cells["A1"].Formula = "=TRIM(B1)";
        Assert.Equal("Hello world", sheet.Cells["A1"].Data.Value);
    }
}