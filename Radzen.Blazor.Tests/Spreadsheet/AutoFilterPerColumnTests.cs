using System.Linq;
using Radzen.Documents.Spreadsheet;
using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

#nullable enable

// Contract for the new per-column filter API on AutoFilter (Pass 2B).
// Column indices are RELATIVE to the auto-filter range.
public class AutoFilterPerColumnTests
{
    private static (Workbook wb, Worksheet ws, AutoFilter af) Build(int rows = 11, int cols = 3)
    {
        var wb = new Workbook();
        var ws = wb.AddSheet("Sheet1", rows, cols);
        ws.Cells[0, 0].SetValue("Name");
        ws.Cells[0, 1].SetValue("Region");
        ws.Cells[0, 2].SetValue("Amount");
        for (var r = 1; r < rows; r++)
        {
            ws.Cells[r, 0].SetValue($"Row{r}");
            ws.Cells[r, 1].SetValue(r % 2 == 0 ? "EMEA" : "AMER");
            ws.Cells[r, 2].Value = (double)(r * 10);
        }
        ws.AutoFilter.Range = new RangeRef(new CellRef(0, 0), new CellRef(rows - 1, cols - 1));
        return (wb, ws, ws.AutoFilter);
    }

    private static int VisibleRowsBetween(Worksheet ws, int from, int to)
    {
        var n = 0;
        for (var r = from; r <= to; r++)
            if (!ws.Rows.IsHidden(r)) n++;
        return n;
    }

    // ── ApplyValueFilter ────────────────────────────────────────────────────
    [Fact]
    public void ApplyValueFilter_ShouldHideRowsNotInTheValueList()
    {
        var (_, ws, af) = Build();
        af.ApplyValueFilter(1, ["EMEA"]);

        // Header always visible; only EMEA rows (even row indices 2, 4, 6, 8, 10) visible
        Assert.False(ws.Rows.IsHidden(0));
        for (var r = 1; r < 11; r++)
        {
            var expectedHidden = r % 2 != 0;
            Assert.Equal(expectedHidden, ws.Rows.IsHidden(r));
        }
    }

    [Fact]
    public void ApplyValueFilter_MultipleValues_ShouldKeepAnyMatch()
    {
        var (_, ws, af) = Build();
        af.ApplyValueFilter(1, ["EMEA", "AMER"]);
        // Both values cover all data rows; only header + every data row stays visible
        Assert.Equal(11, VisibleRowsBetween(ws, 0, 10));
    }

    // ── ApplyCustomFilter ───────────────────────────────────────────────────
    [Fact]
    public void ApplyCustomFilter_GreaterThan_ShouldFilterByCriterion()
    {
        var (_, ws, af) = Build();
        af.ApplyCustomFilter(2, new GreaterThanCriterion { Column = 2, Value = 50.0 });

        // Amount = 10..100; >50 keeps rows 6..10
        for (var r = 1; r <= 5; r++) Assert.True(ws.Rows.IsHidden(r));
        for (var r = 6; r <= 10; r++) Assert.False(ws.Rows.IsHidden(r));
    }

    // ── ApplyTopFilter / ApplyDynamicFilter / ApplyColorFilter ──────────────
    [Fact]
    public void ApplyTopFilter_TopThreeItems_ShouldKeepHighest()
    {
        var (_, ws, af) = Build();
        af.ApplyTopFilter(2, count: 3, percent: false, bottom: false);

        // Top 3 by Amount = rows 8, 9, 10
        for (var r = 1; r <= 7; r++) Assert.True(ws.Rows.IsHidden(r));
        for (var r = 8; r <= 10; r++) Assert.False(ws.Rows.IsHidden(r));
    }

    [Fact]
    public void ApplyDynamicFilter_AboveAverage_ShouldKeepHigh()
    {
        var (_, ws, af) = Build();
        af.ApplyDynamicFilter(2, DynamicFilterType.AboveAverage);

        // Average of 10..100 = 55. Rows with value > 55 = 60,70,80,90,100 (rows 6..10)
        for (var r = 1; r <= 5; r++) Assert.True(ws.Rows.IsHidden(r));
        for (var r = 6; r <= 10; r++) Assert.False(ws.Rows.IsHidden(r));
    }

    [Fact]
    public void ApplyColorFilter_BackgroundColor_ShouldKeepMatching()
    {
        var (_, ws, af) = Build();
        // Tag rows 2 and 5 with yellow
        ws.Cells[2, 0].Format = new Format { BackgroundColor = "#FFFF00" };
        ws.Cells[5, 0].Format = new Format { BackgroundColor = "#FFFF00" };

        af.ApplyColorFilter(0, "#FFFF00", fontColor: false);

        for (var r = 1; r <= 10; r++)
        {
            var expectedVisible = r == 2 || r == 5;
            Assert.Equal(!expectedVisible, ws.Rows.IsHidden(r));
        }
    }

    // ── ClearColumnFilter / GetColumnFilter ─────────────────────────────────
    [Fact]
    public void GetColumnFilter_ShouldReturnAppliedFilter()
    {
        var (_, _, af) = Build();
        af.ApplyValueFilter(1, ["EMEA"]);
        var filter = af.GetColumnFilter(1);
        Assert.NotNull(filter);
        Assert.IsType<InListCriterion>(filter!.Criterion);
    }

    [Fact]
    public void GetColumnFilter_NoFilter_ShouldReturnNull()
    {
        var (_, _, af) = Build();
        Assert.Null(af.GetColumnFilter(1));
    }

    [Fact]
    public void ClearColumnFilter_ShouldRemoveOnlyThatColumn()
    {
        var (_, ws, af) = Build();
        af.ApplyValueFilter(1, ["EMEA"]);
        af.ApplyCustomFilter(2, new GreaterThanCriterion { Column = 2, Value = 50.0 });

        af.ClearColumnFilter(1);

        // Region filter gone; Amount filter still active -> rows with value > 50 visible
        Assert.Null(af.GetColumnFilter(1));
        Assert.NotNull(af.GetColumnFilter(2));

        for (var r = 1; r <= 5; r++) Assert.True(ws.Rows.IsHidden(r));
        for (var r = 6; r <= 10; r++) Assert.False(ws.Rows.IsHidden(r));
    }

    [Fact]
    public void ApplyValueFilter_TwiceOnSameColumn_ShouldReplace()
    {
        var (_, _, af) = Build();
        af.ApplyValueFilter(1, ["EMEA"]);
        af.ApplyValueFilter(1, ["AMER"]);

        // Only one filter on column 1; the AMER one replaces EMEA.
        var filter = af.GetColumnFilter(1);
        Assert.NotNull(filter);
        var inList = Assert.IsType<InListCriterion>(filter!.Criterion);
        Assert.Equal("AMER", inList.Values[0]);
    }

    // ── Range validation ────────────────────────────────────────────────────
    [Fact]
    public void ApplyValueFilter_BeforeSettingRange_ShouldThrow()
    {
        var wb = new Workbook();
        var ws = wb.AddSheet("Sheet1", 5, 3);
        Assert.Throws<System.InvalidOperationException>(() =>
            ws.AutoFilter.ApplyValueFilter(0, ["a"]));
    }

    [Fact]
    public void ApplyValueFilter_ColumnOutOfRange_ShouldThrow()
    {
        var (_, _, af) = Build();
        Assert.Throws<System.ArgumentOutOfRangeException>(() =>
            af.ApplyValueFilter(99, ["x"]));
    }
}
