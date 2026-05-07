using System;
using Radzen.Documents.Spreadsheet;
using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

#nullable enable

// Contract for the new filter criterion subclasses introduced in Pass 2A:
// TopFilterCriterion, DynamicFilterCriterion, CellColorFilterCriterion.
public class FilterCriterionContractTests
{
    private static (Workbook wb, Worksheet ws) Build(int rows, int cols)
    {
        var wb = new Workbook();
        var ws = wb.AddSheet("Sheet1", rows, cols);
        return (wb, ws);
    }

    private static RangeRef Range(int r1, int c1, int r2, int c2) =>
        new(new CellRef(r1, c1), new CellRef(r2, c2));

    // ── TopFilterCriterion ──────────────────────────────────────────────────
    [Fact]
    public void TopFilter_TopN_ShouldMatchHighestNValues()
    {
        var (_, ws) = Build(11, 1);
        ws.Cells[0, 0].SetValue("Header");
        // Values 1..10
        for (var r = 1; r <= 10; r++) ws.Cells[r, 0].Value = (double)r;

        var criterion = new TopFilterCriterion { Column = 0, Count = 3, Bottom = false };
        ws.AddFilter(new SheetFilter(criterion, Range(0, 0, 10, 0)));

        // Only the top 3 (values 8, 9, 10) should be visible.
        Assert.False(ws.Rows.IsHidden(0));   // header always visible
        for (var r = 1; r <= 7; r++) Assert.True(ws.Rows.IsHidden(r));   // 1..7 hidden
        for (var r = 8; r <= 10; r++) Assert.False(ws.Rows.IsHidden(r)); // 8..10 visible
    }

    [Fact]
    public void TopFilter_BottomN_ShouldMatchLowestNValues()
    {
        var (_, ws) = Build(11, 1);
        ws.Cells[0, 0].SetValue("Header");
        for (var r = 1; r <= 10; r++) ws.Cells[r, 0].Value = (double)r;

        ws.AddFilter(new SheetFilter(
            new TopFilterCriterion { Column = 0, Count = 3, Bottom = true },
            Range(0, 0, 10, 0)));

        // Bottom 3 = values 1, 2, 3
        for (var r = 1; r <= 3; r++) Assert.False(ws.Rows.IsHidden(r));
        for (var r = 4; r <= 10; r++) Assert.True(ws.Rows.IsHidden(r));
    }

    [Fact]
    public void TopFilter_TopPercent_ShouldMatchTopFractionOfRows()
    {
        var (_, ws) = Build(11, 1);
        ws.Cells[0, 0].SetValue("Header");
        for (var r = 1; r <= 10; r++) ws.Cells[r, 0].Value = (double)r;

        // Top 30% = top 3 of 10
        ws.AddFilter(new SheetFilter(
            new TopFilterCriterion { Column = 0, Count = 30, Percent = true, Bottom = false },
            Range(0, 0, 10, 0)));

        for (var r = 1; r <= 7; r++) Assert.True(ws.Rows.IsHidden(r));
        for (var r = 8; r <= 10; r++) Assert.False(ws.Rows.IsHidden(r));
    }

    // ── DynamicFilterCriterion ──────────────────────────────────────────────
    [Fact]
    public void DynamicFilter_AboveAverage_ShouldMatchValuesAboveMean()
    {
        var (_, ws) = Build(6, 1);
        ws.Cells[0, 0].SetValue("Header");
        // Values 10, 20, 30, 40, 50 → average 30
        for (var r = 1; r <= 5; r++) ws.Cells[r, 0].Value = (double)(r * 10);

        ws.AddFilter(new SheetFilter(
            new DynamicFilterCriterion { Column = 0, Type = DynamicFilterType.AboveAverage },
            Range(0, 0, 5, 0)));

        // 10, 20, 30 below or equal → hidden; 40, 50 above → visible
        Assert.True(ws.Rows.IsHidden(1));
        Assert.True(ws.Rows.IsHidden(2));
        Assert.True(ws.Rows.IsHidden(3));
        Assert.False(ws.Rows.IsHidden(4));
        Assert.False(ws.Rows.IsHidden(5));
    }

    [Fact]
    public void DynamicFilter_BelowAverage_ShouldMatchValuesBelowMean()
    {
        var (_, ws) = Build(6, 1);
        ws.Cells[0, 0].SetValue("Header");
        for (var r = 1; r <= 5; r++) ws.Cells[r, 0].Value = (double)(r * 10);

        ws.AddFilter(new SheetFilter(
            new DynamicFilterCriterion { Column = 0, Type = DynamicFilterType.BelowAverage },
            Range(0, 0, 5, 0)));

        Assert.False(ws.Rows.IsHidden(1));
        Assert.False(ws.Rows.IsHidden(2));
        Assert.True(ws.Rows.IsHidden(3));
        Assert.True(ws.Rows.IsHidden(4));
        Assert.True(ws.Rows.IsHidden(5));
    }

    [Fact]
    public void DynamicFilter_Today_ShouldMatchTodaysDate()
    {
        var (_, ws) = Build(4, 1);
        ws.Cells[0, 0].SetValue("Header");
        ws.Cells[1, 0].Value = DateTime.Today;
        ws.Cells[2, 0].Value = DateTime.Today.AddDays(-1);
        ws.Cells[3, 0].Value = DateTime.Today.AddDays(1);

        ws.AddFilter(new SheetFilter(
            new DynamicFilterCriterion { Column = 0, Type = DynamicFilterType.Today },
            Range(0, 0, 3, 0)));

        Assert.False(ws.Rows.IsHidden(1));
        Assert.True(ws.Rows.IsHidden(2));
        Assert.True(ws.Rows.IsHidden(3));
    }

    [Fact]
    public void DynamicFilter_ThisMonth_ShouldMatchDatesInCurrentMonth()
    {
        var (_, ws) = Build(4, 1);
        var today = DateTime.Today;
        ws.Cells[0, 0].SetValue("Header");
        ws.Cells[1, 0].Value = today;
        ws.Cells[2, 0].Value = today.AddMonths(-1);
        ws.Cells[3, 0].Value = today.AddMonths(1);

        ws.AddFilter(new SheetFilter(
            new DynamicFilterCriterion { Column = 0, Type = DynamicFilterType.ThisMonth },
            Range(0, 0, 3, 0)));

        Assert.False(ws.Rows.IsHidden(1));
        Assert.True(ws.Rows.IsHidden(2));
        Assert.True(ws.Rows.IsHidden(3));
    }

    [Fact]
    public void DynamicFilter_Quarter1_ShouldMatchJanFebMar()
    {
        var (_, ws) = Build(5, 1);
        ws.Cells[0, 0].SetValue("Header");
        ws.Cells[1, 0].Value = new DateTime(2024, 2, 14);
        ws.Cells[2, 0].Value = new DateTime(2024, 4, 1);
        ws.Cells[3, 0].Value = new DateTime(2024, 3, 31);
        ws.Cells[4, 0].Value = new DateTime(2024, 7, 15);

        ws.AddFilter(new SheetFilter(
            new DynamicFilterCriterion { Column = 0, Type = DynamicFilterType.Quarter1 },
            Range(0, 0, 4, 0)));

        Assert.False(ws.Rows.IsHidden(1));  // Feb
        Assert.True(ws.Rows.IsHidden(2));   // Apr
        Assert.False(ws.Rows.IsHidden(3));  // Mar
        Assert.True(ws.Rows.IsHidden(4));   // Jul
    }

    // ── CellColorFilterCriterion ────────────────────────────────────────────
    [Fact]
    public void CellColorFilter_ShouldMatchCellsWithMatchingBackground()
    {
        var (_, ws) = Build(5, 1);
        ws.Cells[0, 0].SetValue("Header");
        ws.Cells[1, 0].SetValue("a");
        ws.Cells[2, 0].SetValue("b"); ws.Cells[2, 0].Format = new Format { BackgroundColor = "#FFFF00" };
        ws.Cells[3, 0].SetValue("c");
        ws.Cells[4, 0].SetValue("d"); ws.Cells[4, 0].Format = new Format { BackgroundColor = "#FFFF00" };

        ws.AddFilter(new SheetFilter(
            new CellColorFilterCriterion { Column = 0, Color = "#FFFF00" },
            Range(0, 0, 4, 0)));

        Assert.True(ws.Rows.IsHidden(1));
        Assert.False(ws.Rows.IsHidden(2));
        Assert.True(ws.Rows.IsHidden(3));
        Assert.False(ws.Rows.IsHidden(4));
    }

    [Fact]
    public void CellColorFilter_FontColor_ShouldMatchCellsWithMatchingFontColor()
    {
        var (_, ws) = Build(4, 1);
        ws.Cells[0, 0].SetValue("Header");
        ws.Cells[1, 0].SetValue("a");
        ws.Cells[2, 0].SetValue("b"); ws.Cells[2, 0].Format = new Format { Color = "#FF0000" };
        ws.Cells[3, 0].SetValue("c");

        ws.AddFilter(new SheetFilter(
            new CellColorFilterCriterion { Column = 0, Color = "#FF0000", FontColor = true },
            Range(0, 0, 3, 0)));

        Assert.True(ws.Rows.IsHidden(1));
        Assert.False(ws.Rows.IsHidden(2));
        Assert.True(ws.Rows.IsHidden(3));
    }

    // ── Visitor pattern coverage ────────────────────────────────────────────
    [Fact]
    public void NewCriteria_ShouldDispatchThroughVisitor()
    {
        var visited = new System.Collections.Generic.List<string>();
        var visitor = new RecordingVisitor(visited);

        new TopFilterCriterion { Column = 0, Count = 5 }.Accept(visitor);
        new DynamicFilterCriterion { Column = 0, Type = DynamicFilterType.Today }.Accept(visitor);
        new CellColorFilterCriterion { Column = 0, Color = "#FFF" }.Accept(visitor);

        Assert.Equal(["TopFilter", "DynamicFilter", "CellColorFilter"], visited);
    }

    private sealed class RecordingVisitor(System.Collections.Generic.List<string> log) : FilterCriterionVisitorBase
    {
        public override void Visit(TopFilterCriterion criterion) => log.Add("TopFilter");
        public override void Visit(DynamicFilterCriterion criterion) => log.Add("DynamicFilter");
        public override void Visit(CellColorFilterCriterion criterion) => log.Add("CellColorFilter");
    }
}
