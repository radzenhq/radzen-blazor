using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class XMatchFunctionTests
{
    readonly Worksheet sheet = new(10, 10);

    [Fact]
    public void ShouldMatchExactByDefault()
    {
        sheet.Cells["A1"].Value = "Hat";
        sheet.Cells["A2"].Value = "Gloves";
        sheet.Cells["A3"].Value = "Scarf";

        sheet.Cells["B1"].Formula = "=XMATCH(\"Gloves\",A1:A3)";
        Assert.Equal(2d, sheet.Cells["B1"].Value);
    }

    [Fact]
    public void ShouldReturnNAWhenNotFound()
    {
        sheet.Cells["A1"].Value = "Hat";
        sheet.Cells["B1"].Formula = "=XMATCH(\"Gloves\",A1:A1)";
        Assert.Equal(CellError.NA, sheet.Cells["B1"].Value);
    }

    [Fact]
    public void ShouldMatchNextSmaller()
    {
        sheet.Cells["A1"].Value = 1;
        sheet.Cells["A2"].Value = 5;
        sheet.Cells["A3"].Value = 10;

        // match_mode -1: exact or next smaller; 7 -> 5 at position 2
        sheet.Cells["B1"].Formula = "=XMATCH(7,A1:A3,-1)";
        Assert.Equal(2d, sheet.Cells["B1"].Value);
    }

    [Fact]
    public void ShouldMatchWildcard()
    {
        sheet.Cells["A1"].Value = "apple";
        sheet.Cells["A2"].Value = "banana";

        sheet.Cells["B1"].Formula = "=XMATCH(\"ban*\",A1:A2,2)";
        Assert.Equal(2d, sheet.Cells["B1"].Value);
    }
}
