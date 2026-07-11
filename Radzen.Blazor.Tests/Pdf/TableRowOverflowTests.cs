#nullable enable
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// A row with more cells than the table's resolved column count used to silently
// drop the trailing cells; it must now fail loudly instead.
public class TableRowOverflowTests
{
    [Fact]
    public void Row_WithMoreCellsThanColumns_ThrowsDescriptively()
    {
        var fonts = TableLayoutSupport.Fonts();
        var table = new Table();
        table.Columns.Add(Unit.FromPoint(100));
        table.Columns.Add(Unit.FromPoint(100));

        var row = table.Rows.Add();
        TableLayoutSupport.Fill(row.Cells[0], "A");
        TableLayoutSupport.Fill(row.Cells[1], "B");
        TableLayoutSupport.Fill(row.Cells.AddCell(), "Extra");

        var exception = Assert.Throws<System.InvalidOperationException>(
            () => TableLayout.Layout(table, 400, fonts));

        Assert.Contains("0", exception.Message);
        Assert.Contains("2", exception.Message);
    }

    [Fact]
    public void Row_WithCellCountMatchingColumns_LaysOutFine()
    {
        var fonts = TableLayoutSupport.Fonts();
        var table = new Table();
        table.Columns.Add(Unit.FromPoint(100));
        table.Columns.Add(Unit.FromPoint(100));

        var row = table.Rows.Add();
        TableLayoutSupport.Fill(row.Cells[0], "A");
        TableLayoutSupport.Fill(row.Cells[1], "B");

        var laid = TableLayout.Layout(table, 400, fonts);

        Assert.Equal(2, laid.Cells.Count);
    }
}
