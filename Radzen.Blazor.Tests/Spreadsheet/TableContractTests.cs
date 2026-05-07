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
        Assert.False(t.ShowTotalsRow);
        Assert.True(t.ShowFilterButton);
        Assert.True(t.ShowBandedRows);
        Assert.False(t.ShowBandedColumns);
        Assert.False(t.HighlightFirstColumn);
        Assert.False(t.HighlightLastColumn);

        t.ShowTotalsRow = true;
        t.ShowBandedColumns = true;
        t.HighlightFirstColumn = true;
        t.HighlightLastColumn = true;
        t.ShowBandedRows = false;

        Assert.True(t.ShowTotalsRow);
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
    public void Table_DataRange_ShouldExcludeHeaderAndTotalsRows()
    {
        var (_, ws) = NewSheet();
        var t = ws.AddTable("T", Range(0, 0, 5, 4));
        Assert.Equal(Range(1, 0, 5, 4), t.DataRange);

        t.ShowTotalsRow = true;
        Assert.Equal(Range(1, 0, 4, 4), t.DataRange);
    }

    [Fact]
    public void Table_TotalsRowRange_ShouldBeLastRow_WhenTotalsShown()
    {
        var (_, ws) = NewSheet();
        var t = ws.AddTable("T", Range(0, 0, 5, 4));
        Assert.Null(t.TotalsRowRange);

        t.ShowTotalsRow = true;
        Assert.Equal(Range(5, 0, 5, 4), t.TotalsRowRange);
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

        t.ShowTotalsRow = true;
        t.Columns[2].TotalsCalculation = TotalsCalculation.Sum;
        Assert.Equal(TotalsCalculation.Sum, t.Columns[2].TotalsCalculation);
    }

    [Fact]
    public void TableColumn_TotalsCalculation_ShouldEmitFormulaIntoTotalsCell()
    {
        var (_, ws) = NewSheet();
        var t = ws.AddTable("Sales", Range(0, 0, 5, 4));
        t.ShowTotalsRow = true;
        t.Columns[2].TotalsCalculation = TotalsCalculation.Sum;

        // The totals cell should contain a SUBTOTAL formula referencing this column.
        var totalsCell = ws.Cells[5, 2];
        Assert.NotNull(totalsCell.Formula);
        Assert.Contains("SUBTOTAL", totalsCell.Formula!, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TableColumn_CalculatedFormula_ShouldFillEntireColumn()
    {
        var (_, ws) = NewSheet();
        var t = ws.AddTable("Sales", Range(0, 0, 5, 4));
        t.Columns[4].CalculatedFormula = "=[@[Q1]]+[@[Q2]]";
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
}
