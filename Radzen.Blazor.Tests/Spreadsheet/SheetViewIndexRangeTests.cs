using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class SheetViewIndexRangeTests
{
    [Fact]
    public void GetColumnRange_ShouldIncludeLastColumn_WhenViewportCoversEntireSheet()
    {
        // 5 columns, each 100px wide = 500px total. Viewport is 0..600 (wider than content).
        var sheet = new Worksheet(5, 5);
        var view = new SheetView(sheet);

        // Request a range that covers the entire width.
        // ColumnHeaderOffset is 100, so pass start=0, end=100+500+100 to cover everything.
        var range = view.GetColumnRange(0, 700);

        // The last column (index 4) must be included
        Assert.Equal(4, range.End);
    }

    [Fact]
    public void GetRowRange_ShouldIncludeLastRow_WhenViewportCoversEntireSheet()
    {
        // 5 rows, each 24px tall = 120px total. Viewport is 0..300 (taller than content).
        var sheet = new Worksheet(5, 5);
        var view = new SheetView(sheet);

        var range = view.GetRowRange(0, 300);

        // The last row (index 4) must be included
        Assert.Equal(4, range.End);
    }

    [Fact]
    public void GetColumnRange_ShouldIncludeLastColumn_WhenScrolledToEnd()
    {
        // 10 columns at 100px each = 1000px. Viewport shows last 200px.
        var sheet = new Worksheet(5, 10);
        var view = new SheetView(sheet);

        // Scroll to the end: start at 800px + header offset, end at 1000px + header offset
        var range = view.GetColumnRange(900, 1200);

        Assert.Equal(9, range.End);
    }

    [Fact]
    public void GetRowRange_ShouldIncludeLastRow_WhenScrolledToEnd()
    {
        // 10 rows at 24px each = 240px. Viewport shows last 50px.
        var sheet = new Worksheet(10, 5);
        var view = new SheetView(sheet);

        // Scroll to the end
        var range = view.GetRowRange(210, 300);

        Assert.Equal(9, range.End);
    }

    [Fact]
    public void GetColumnRange_LastColumnStartIndex_WhenOnlyLastColumnVisible()
    {
        // 5 columns at 100px. Viewport starts at the last column.
        var sheet = new Worksheet(5, 5);
        var view = new SheetView(sheet);

        // ColumnHeaderOffset=100, columns are 0..4 at 100px each.
        // Column 4 starts at pixel 400+100=500. Request viewport starting there.
        var range = view.GetColumnRange(500, 700);

        // Start and end should both be the last column
        Assert.Equal(4, range.Start);
        Assert.Equal(4, range.End);
    }
}
