using System.IO;
using Radzen.Documents.Spreadsheet;
using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

#nullable enable

// Regression: XlsxReader created a fixed 100x100 worksheet, so loading a file whose
// used range exceeded 100 rows/columns threw "Row index is out of range." The reader
// must size each sheet from its actual content (with 100x100 as the minimum grid).
public class XlsxReaderSheetSizeTests
{
    private static MemoryStream Save(Workbook wb)
    {
        var ms = new MemoryStream();
        wb.SaveToStream(ms);
        ms.Position = 0;
        return ms;
    }

    [Fact]
    public void RoundTrip_SheetTallerThan100Rows_LoadsAndPreservesValue()
    {
        var wb = new Workbook();
        var sheet = wb.AddSheet("Tall", 200, 10);
        sheet.Cells[149, 0].Value = "row150"; // A150 - beyond the old 100-row cap

        using var ms = Save(wb);

        var loaded = Workbook.LoadFromStream(ms).Sheets[0]; // previously threw here

        Assert.True(loaded.RowCount >= 150, $"RowCount {loaded.RowCount}");
        Assert.Equal("row150", loaded.Cells[149, 0].Value);
    }

    [Fact]
    public void RoundTrip_SheetWiderThan100Columns_LoadsAndPreservesValue()
    {
        var wb = new Workbook();
        var sheet = wb.AddSheet("Wide", 10, 130);
        sheet.Cells[0, 119].Value = "col120"; // DP1 - beyond the old 100-column cap

        using var ms = Save(wb);

        var loaded = Workbook.LoadFromStream(ms).Sheets[0];

        Assert.True(loaded.ColumnCount >= 120, $"ColumnCount {loaded.ColumnCount}");
        Assert.Equal("col120", loaded.Cells[0, 119].Value);
    }

    [Fact]
    public void RoundTrip_SmallSheet_KeepsDefault100x100Grid()
    {
        var wb = new Workbook();
        var sheet = wb.AddSheet("Small", 5, 5);
        sheet.Cells[0, 0].Value = "hi";

        using var ms = Save(wb);

        var loaded = Workbook.LoadFromStream(ms).Sheets[0];

        // Floor preserved so a nearly-empty file still renders a full default grid.
        Assert.Equal(100, loaded.RowCount);
        Assert.Equal(100, loaded.ColumnCount);
        Assert.Equal("hi", loaded.Cells[0, 0].Value);
    }
}
