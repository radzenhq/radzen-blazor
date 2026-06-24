using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class MatchFunctionTests
{
    readonly Worksheet sheet = new(10, 10);

    [Fact]
    public void ShouldMatchExact()
    {
        sheet.Cells["A1"].Value = "Hat";
        sheet.Cells["A2"].Value = "Gloves";
        sheet.Cells["A3"].Value = "Scarf";

        sheet.Cells["B1"].Formula = "=MATCH(\"Gloves\",A1:A3,0)";

        Assert.Equal(2d, sheet.Cells["B1"].Value);
    }

    [Fact]
    public void ShouldMatchExactCaseInsensitive()
    {
        sheet.Cells["A1"].Value = "Hat";
        sheet.Cells["A2"].Value = "Gloves";

        sheet.Cells["B1"].Formula = "=MATCH(\"gloves\",A1:A2,0)";

        Assert.Equal(2d, sheet.Cells["B1"].Value);
    }

    [Fact]
    public void ShouldReturnNAWhenExactNotFound()
    {
        sheet.Cells["A1"].Value = "Hat";
        sheet.Cells["A2"].Value = "Gloves";

        sheet.Cells["B1"].Formula = "=MATCH(\"Scarf\",A1:A2,0)";

        Assert.Equal(CellError.NA, sheet.Cells["B1"].Value);
    }

    [Fact]
    public void ShouldMatchApproximateAscending()
    {
        sheet.Cells["A1"].Value = 1;
        sheet.Cells["A2"].Value = 5;
        sheet.Cells["A3"].Value = 10;

        // largest value <= 7 is 5 at position 2
        sheet.Cells["B1"].Formula = "=MATCH(7,A1:A3,1)";

        Assert.Equal(2d, sheet.Cells["B1"].Value);
    }

    [Fact]
    public void ShouldMatchApproximateDescending()
    {
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["A2"].Value = 5;
        sheet.Cells["A3"].Value = 1;

        // smallest value >= 7 is 10 at position 1
        sheet.Cells["B1"].Formula = "=MATCH(7,A1:A3,-1)";

        Assert.Equal(1d, sheet.Cells["B1"].Value);
    }

    [Fact]
    public void ShouldDefaultToApproximateAscending()
    {
        sheet.Cells["A1"].Value = 1;
        sheet.Cells["A2"].Value = 5;
        sheet.Cells["A3"].Value = 10;

        sheet.Cells["B1"].Formula = "=MATCH(5,A1:A3)";

        Assert.Equal(2d, sheet.Cells["B1"].Value);
    }

    [Fact]
    public void ShouldMatchWildcard()
    {
        sheet.Cells["A1"].Value = "apple";
        sheet.Cells["A2"].Value = "banana";

        sheet.Cells["B1"].Formula = "=MATCH(\"ban*\",A1:A2,0)";

        Assert.Equal(2d, sheet.Cells["B1"].Value);
    }
}
