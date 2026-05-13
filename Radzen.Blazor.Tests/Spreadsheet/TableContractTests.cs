using System.Linq;
using Radzen.Documents.Spreadsheet;
using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

#nullable enable

// Contract for the new Table + TableColumn API. One [Fact] per behavior.
// All tests fail today; each turns green as Pass 1B lands.
public class TableContractTests
{
    private static (Workbook wb, Worksheet ws) NewSheet(int rows = 10, int cols = 5)
    {
        var wb = new Workbook();
        var ws = wb.AddSheet("Sheet1", rows, cols);
        // Header row
        ws.Cells[0, 0].SetValue("Region");
        ws.Cells[0, 1].SetValue("Product");
        ws.Cells[0, 2].SetValue("Q1");
        ws.Cells[0, 3].SetValue("Q2");
        ws.Cells[0, 4].SetValue("Total");
        // Data rows
        for (var r = 1; r <= 5; r++)
        {
            ws.Cells[r, 0].SetValue($"R{r}");
            ws.Cells[r, 1].SetValue($"P{r}");
            ws.Cells[r, 2].Value = (double)(r * 100);
            ws.Cells[r, 3].Value = (double)(r * 200);
        }
        return (wb, ws);
    }

    private static RangeRef Range(int r1, int c1, int r2, int c2) =>
        new(new CellRef(r1, c1), new CellRef(r2, c2));

    // ── AddTable signature ──────────────────────────────────────────────────
    [Fact]
    public void AddTable_ShouldReturnCreatedTable()
    {
        var (_, ws) = NewSheet();
        var table = ws.AddTable("Sales", Range(0, 0, 5, 4));
        Assert.NotNull(table);
        Assert.Same(ws, table.Worksheet);
        Assert.Equal("Sales", table.Name);
    }

    [Fact]
    public void AddTable_HasHeaders_ShouldDefaultToTrue()
    {
        var (_, ws) = NewSheet();
        var table = ws.AddTable("T", Range(0, 0, 5, 4));
        Assert.True(table.ShowHeaderRow);
    }

    [Fact]
    public void AddTable_ShouldAcceptHasHeadersFalse()
    {
        var (_, ws) = NewSheet();
        var table = ws.AddTable("T", Range(0, 0, 5, 4), hasHeaders: false);
        Assert.False(table.ShowHeaderRow);
    }

    // ── Identity ────────────────────────────────────────────────────────────
    [Fact]
    public void Table_Name_ShouldRoundTrip()
    {
        var (_, ws) = NewSheet();
        var table = ws.AddTable("Sales", Range(0, 0, 5, 4));
        table.Name = "Renamed";
        Assert.Equal("Renamed", table.Name);
    }

    [Fact]
    public void Table_DisplayName_ShouldDefaultToName_AndOverride()
    {
        var (_, ws) = NewSheet();
        var table = ws.AddTable("Sales", Range(0, 0, 5, 4));
        Assert.Equal("Sales", table.DisplayName);
        table.DisplayName = "Sales (Q1-Q2)";
        Assert.Equal("Sales (Q1-Q2)", table.DisplayName);
    }

    [Fact]
    public void Workbook_TableNames_MustBeUnique()
    {
        var (wb, ws) = NewSheet();
        ws.AddTable("Sales", Range(0, 0, 5, 4));
        Assert.Throws<System.ArgumentException>(() =>
            ws.AddTable("Sales", Range(0, 0, 5, 4)));
    }

    // ── Toggle properties ───────────────────────────────────────────────────
    [Fact]
    public void Table_Toggles_ShouldDefaultSensibly_AndRoundTrip()
    {
        var (_, ws) = NewSheet();
        var t = ws.AddTable("T", Range(0, 0, 5, 4));

        Assert.True(t.ShowHeaderRow);
        Assert.False(t.ShowTotals);
        Assert.True(t.ShowFilterButton);
        Assert.True(t.ShowBandedRows);
        Assert.False(t.ShowBandedColumns);
        Assert.False(t.HighlightFirstColumn);
        Assert.False(t.HighlightLastColumn);

        t.ShowTotals = true;
        t.ShowBandedColumns = true;
        t.HighlightFirstColumn = true;
        t.HighlightLastColumn = true;
        t.ShowBandedRows = false;

        Assert.True(t.ShowTotals);
        Assert.True(t.ShowBandedColumns);
        Assert.True(t.HighlightFirstColumn);
        Assert.True(t.HighlightLastColumn);
        Assert.False(t.ShowBandedRows);
    }

    [Fact]
    public void Table_TableStyle_ShouldRoundTrip()
    {
        var (_, ws) = NewSheet();
        var t = ws.AddTable("T", Range(0, 0, 5, 4));
        Assert.Equal("TableStyleMedium2", t.TableStyle);
        t.TableStyle = "TableStyleLight15";
        Assert.Equal("TableStyleLight15", t.TableStyle);
    }

    // ── Computed sub-ranges ─────────────────────────────────────────────────
    [Fact]
    public void Table_HeaderRowRange_ShouldBeFirstRow_WhenHeadersShown()
    {
        var (_, ws) = NewSheet();
        var t = ws.AddTable("T", Range(0, 0, 5, 4));
        Assert.Equal(Range(0, 0, 0, 4), t.HeaderRowRange);
    }

    [Fact]
    public void Table_HeaderRowRange_ShouldBeNull_WhenHeadersHidden()
    {
        var (_, ws) = NewSheet();
        var t = ws.AddTable("T", Range(0, 0, 5, 4), hasHeaders: false);
        Assert.Null(t.HeaderRowRange);
    }

    [Fact]
    public void Table_DataBodyRange_ShouldNotShrink_WhenTotalsAdded()
    {
        // Excel parity (verified via COM probe): ShowTotals=true ADDS a new row
        // below the existing range; data rows are unchanged.
        var (_, ws) = NewSheet();
        var t = ws.AddTable("T", Range(0, 0, 5, 4));
        Assert.Equal(Range(1, 0, 5, 4), t.DataBodyRange);

        t.ShowTotals = true;
        Assert.Equal(Range(1, 0, 5, 4), t.DataBodyRange);   // unchanged
        Assert.Equal(Range(0, 0, 6, 4), t.Range);       // range expanded by one
    }

    [Fact]
    public void Table_TotalsRowRange_ShouldBeNewRowBelowData_WhenTotalsShown()
    {
        var (_, ws) = NewSheet();
        var t = ws.AddTable("T", Range(0, 0, 5, 4));
        Assert.Null(t.TotalsRowRange);

        t.ShowTotals = true;
        // Range was 0..5, totals added at row 6.
        Assert.Equal(Range(6, 0, 6, 4), t.TotalsRowRange);
    }

    [Fact]
    public void ShowTotals_FlipToTrue_ShouldWriteTotalLabelInFirstColumn()
    {
        var (_, ws) = NewSheet();
        var t = ws.AddTable("T", Range(0, 0, 5, 4));
        t.ShowTotals = true;
        Assert.Equal("Total", ws.Cells[6, 0].Value);
    }

    [Fact]
    public void ShowTotals_FlipToFalse_ShouldShrinkRangeAndClearRow()
    {
        var (_, ws) = NewSheet();
        var t = ws.AddTable("T", Range(0, 0, 5, 4));
        t.ShowTotals = true;
        Assert.Equal(Range(0, 0, 6, 4), t.Range);

        t.ShowTotals = false;
        Assert.Equal(Range(0, 0, 5, 4), t.Range);
        Assert.Null(t.TotalsRowRange);
        // Row that was the totals row is now cleared (it's outside the table).
        for (var c = 0; c < 5; c++)
        {
            Assert.Null(ws.Cells[6, c].Value);
            Assert.Null(ws.Cells[6, c].Formula);
        }
    }

    // ── Columns collection ──────────────────────────────────────────────────
    [Fact]
    public void Table_Columns_ShouldReflectRangeWidth()
    {
        var (_, ws) = NewSheet();
        var t = ws.AddTable("T", Range(0, 0, 5, 4));
        Assert.Equal(5, t.Columns.Count);
    }

    [Fact]
    public void TableColumn_Name_ShouldDefaultToHeaderCellValue_WhenHeadersShown()
    {
        var (_, ws) = NewSheet();
        var t = ws.AddTable("T", Range(0, 0, 5, 4));
        Assert.Equal("Region", t.Columns[0].Name);
        Assert.Equal("Product", t.Columns[1].Name);
        Assert.Equal("Total", t.Columns[4].Name);
    }

    [Fact]
    public void TableColumn_Name_ShouldDefaultToColumn1ColumnN_WhenNoHeaders()
    {
        var (_, ws) = NewSheet();
        var t = ws.AddTable("T", Range(0, 0, 5, 4), hasHeaders: false);
        Assert.Equal("Column1", t.Columns[0].Name);
        Assert.Equal("Column5", t.Columns[4].Name);
    }

    [Fact]
    public void TableColumn_Range_ShouldSpanDataRows()
    {
        var (_, ws) = NewSheet();
        var t = ws.AddTable("T", Range(0, 0, 5, 4));
        Assert.Equal(Range(1, 2, 5, 2), t.Columns[2].Range);
    }

    [Fact]
    public void TableColumn_TotalsCalculation_ShouldDefaultToNone_AndRoundTrip()
    {
        var (_, ws) = NewSheet();
        var t = ws.AddTable("T", Range(0, 0, 5, 4));
        Assert.Equal(TotalsCalculation.None, t.Columns[2].TotalsCalculation);

        t.ShowTotals = true;
        t.Columns[2].TotalsCalculation = TotalsCalculation.Sum;
        Assert.Equal(TotalsCalculation.Sum, t.Columns[2].TotalsCalculation);
    }

    [Fact]
    public void TableColumn_TotalsCalculation_ShouldEmitFormulaIntoTotalsCell()
    {
        var (_, ws) = NewSheet();
        var t = ws.AddTable("Sales", Range(0, 0, 5, 4));
        t.ShowTotals = true;       // adds a new row 6 below the data
        t.Columns[2].TotalsCalculation = TotalsCalculation.Sum;

        // The totals cell at the new bottom of the table contains a SUBTOTAL formula.
        var totalsCell = ws.Cells[6, 2];
        Assert.NotNull(totalsCell.Formula);
        Assert.Contains("SUBTOTAL", totalsCell.Formula!, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TableColumn_Formula_ShouldFillEntireColumn()
    {
        var (_, ws) = NewSheet();
        var t = ws.AddTable("Sales", Range(0, 0, 5, 4));
        t.Columns[4].Formula = "=[@[Q1]]+[@[Q2]]";
        // Setting the calculated formula populates every data row of column 4.
        for (var r = 1; r <= 5; r++)
        {
            Assert.False(string.IsNullOrEmpty(ws.Cells[r, 4].Formula),
                $"row {r} expected a formula");
        }
    }

    // ── Lifecycle ───────────────────────────────────────────────────────────
    [Fact]
    public void Table_Resize_ShouldChangeRange()
    {
        var (_, ws) = NewSheet();
        var t = ws.AddTable("T", Range(0, 0, 5, 4));
        t.Resize(Range(0, 0, 7, 4));
        Assert.Equal(Range(0, 0, 7, 4), t.Range);
    }

    [Fact]
    public void Table_ConvertToRange_ShouldRemoveTableButKeepData()
    {
        var (_, ws) = NewSheet();
        var t = ws.AddTable("T", Range(0, 0, 5, 4));
        t.ConvertToRange();
        Assert.Empty(ws.Tables);
        // Data preserved
        Assert.Equal("Region", ws.Cells[0, 0].Value);
        Assert.Equal("R1", ws.Cells[1, 0].Value);
    }

    [Fact]
    public void Table_Delete_ShouldRemoveTableAndKeepCellsByDefault()
    {
        var (_, ws) = NewSheet();
        var t = ws.AddTable("T", Range(0, 0, 5, 4));
        t.Delete();
        Assert.Empty(ws.Tables);
        // Default Delete preserves cell content (Excel parity).
        Assert.Equal("Region", ws.Cells[0, 0].Value);
    }

    // ── ShowHeaderRow runtime toggle (Excel parity) ─────────────────────────
    // Pinned via COM probe: when Excel flips ShowHeaders to false, the table
    // range shrinks, the header row's cells (values + formatting) are cleared,
    // the AutoFilter is removed, and ShowAutoFilter becomes false.

    [Fact]
    public void ShowHeaderRow_FlipToFalse_ShouldShrinkRange()
    {
        var (_, ws) = NewSheet();
        var t = ws.AddTable("Sales", Range(0, 0, 5, 4));
        Assert.Equal(Range(0, 0, 5, 4), t.Range);

        t.ShowHeaderRow = false;

        Assert.Equal(Range(1, 0, 5, 4), t.Range);
        Assert.Null(t.HeaderRowRange);
        Assert.Equal(Range(1, 0, 5, 4), t.DataBodyRange);
    }

    [Fact]
    public void ShowHeaderRow_FlipToFalse_ShouldClearHeaderRowCells()
    {
        var (_, ws) = NewSheet();
        var t = ws.AddTable("Sales", Range(0, 0, 5, 4));
        Assert.Equal("Region", ws.Cells[0, 0].Value);

        t.ShowHeaderRow = false;

        // Each cell that was in the now-removed header row is cleared.
        for (var c = 0; c < 5; c++)
        {
            Assert.Null(ws.Cells[0, c].Value);
            Assert.Null(ws.Cells[0, c].Formula);
        }
    }

    [Fact]
    public void ShowHeaderRow_FlipToFalse_ShouldDisableFilterButton()
    {
        var (_, ws) = NewSheet();
        var t = ws.AddTable("Sales", Range(0, 0, 5, 4));
        Assert.True(t.ShowFilterButton);

        t.ShowHeaderRow = false;

        Assert.False(t.ShowFilterButton);
    }

    [Fact]
    public void ShowHeaderRow_FlipToTrue_ShouldExpandRangeAndWriteDefaultHeaders()
    {
        var (_, ws) = NewSheet();
        // Build a header-less table starting at row 1 so there is room above to expand.
        var t = ws.AddTable("Sales", Range(1, 0, 5, 4), hasHeaders: false);
        Assert.Equal(Range(1, 0, 5, 4), t.Range);

        t.ShowHeaderRow = true;

        Assert.Equal(Range(0, 0, 5, 4), t.Range);
        Assert.Equal(Range(0, 0, 0, 4), t.HeaderRowRange);
        Assert.Equal("Column1", ws.Cells[0, 0].Value);
        Assert.Equal("Column2", ws.Cells[0, 1].Value);
        Assert.Equal("Column5", ws.Cells[0, 4].Value);
    }

    [Fact]
    public void ShowHeaderRow_FlipToTrue_ShouldEnableFilterButton()
    {
        var (_, ws) = NewSheet();
        var t = ws.AddTable("Sales", Range(1, 0, 5, 4), hasHeaders: false);
        // hasHeaders=false implies no filter button needed; verify state then flip.
        t.ShowFilterButton = false;

        t.ShowHeaderRow = true;

        Assert.True(t.ShowFilterButton);
    }
}
