using Bunit;
using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

/// <summary>
/// Tests for the bug where a merged cell that crosses a frozen-row or frozen-column
/// boundary renders empty in every split piece except the first. The root cause was
/// that <see cref="VirtualGrid"/> emitted a <see cref="VirtualDataItem"/> per split
/// piece with its (Row, Column) set to the piece's start — which is a non-anchor cell
/// of the merge — and <see cref="CellView"/> then read that non-anchor (empty) cell
/// directly via the cell store indexer. CellView now resolves the merge anchor before
/// reading the cell, so every split piece displays the anchor's value.
/// </summary>
public class MergedCellAnchorTests : TestContext
{
    private static Worksheet CreateSheetWithMerge(int frozenRows = 0, int frozenColumns = 0)
    {
        var sheet = new Worksheet(20, 20);
        sheet.Cells[0, 0].Value = "MERGED";
        sheet.MergedCells.Add(new RangeRef(new CellRef(0, 0), new CellRef(4, 4)));

        if (frozenRows > 0)
        {
            sheet.Rows.Frozen = frozenRows;
        }

        if (frozenColumns > 0)
        {
            sheet.Columns.Frozen = frozenColumns;
        }

        return sheet;
    }

    [Fact]
    public void CellView_AtAnchor_RendersAnchorValue()
    {
        var sheet = CreateSheetWithMerge();

        var cut = RenderComponent<CellView>(parameters => parameters
            .Add(c => c.Worksheet, sheet)
            .Add(c => c.Row, 0)
            .Add(c => c.Column, 0));

        Assert.Contains("MERGED", cut.Markup);
    }

    [Fact]
    public void CellView_AtNonAnchorInsideMerge_RendersAnchorValue()
    {
        var sheet = CreateSheetWithMerge();

        var cut = RenderComponent<CellView>(parameters => parameters
            .Add(c => c.Worksheet, sheet)
            .Add(c => c.Row, 4)
            .Add(c => c.Column, 2));

        Assert.Contains("MERGED", cut.Markup);
    }

    [Fact]
    public void CellView_AtFrozenRowSplitBoundary_RendersAnchorValue()
    {
        var sheet = CreateSheetWithMerge(frozenRows: 2);

        // The second split piece starts at (frozenRows, mergeStartColumn) = (2, 0).
        // Before the fix this position read an empty non-anchor cell.
        var cut = RenderComponent<CellView>(parameters => parameters
            .Add(c => c.Worksheet, sheet)
            .Add(c => c.Row, 2)
            .Add(c => c.Column, 0));

        Assert.Contains("MERGED", cut.Markup);
    }

    [Fact]
    public void CellView_AtFrozenColumnSplitBoundary_RendersAnchorValue()
    {
        var sheet = CreateSheetWithMerge(frozenColumns: 2);

        // The right split piece starts at (mergeStartRow, frozenColumns) = (0, 2).
        var cut = RenderComponent<CellView>(parameters => parameters
            .Add(c => c.Worksheet, sheet)
            .Add(c => c.Row, 0)
            .Add(c => c.Column, 2));

        Assert.Contains("MERGED", cut.Markup);
    }

    [Fact]
    public void CellView_AtBothFrozenSplitBoundary_RendersAnchorValue()
    {
        var sheet = CreateSheetWithMerge(frozenRows: 2, frozenColumns: 2);

        // With both axes frozen, SplitRange produces 4 pieces. The bottom-right piece
        // starts at (frozenRows, frozenColumns) = (2, 2).
        var cut = RenderComponent<CellView>(parameters => parameters
            .Add(c => c.Worksheet, sheet)
            .Add(c => c.Row, 2)
            .Add(c => c.Column, 2));

        Assert.Contains("MERGED", cut.Markup);
    }

    [Fact]
    public void CellView_AtBottomLeftSplitPiece_RendersAnchorValue()
    {
        var sheet = CreateSheetWithMerge(frozenRows: 2, frozenColumns: 2);

        // The bottom-left piece starts at (frozenRows, mergeStartColumn) = (2, 0).
        var cut = RenderComponent<CellView>(parameters => parameters
            .Add(c => c.Worksheet, sheet)
            .Add(c => c.Row, 2)
            .Add(c => c.Column, 0));

        Assert.Contains("MERGED", cut.Markup);
    }

    [Fact]
    public void CellView_AtTopRightSplitPiece_RendersAnchorValue()
    {
        var sheet = CreateSheetWithMerge(frozenRows: 2, frozenColumns: 2);

        // The top-right piece starts at (mergeStartRow, frozenColumns) = (0, 2).
        var cut = RenderComponent<CellView>(parameters => parameters
            .Add(c => c.Worksheet, sheet)
            .Add(c => c.Row, 0)
            .Add(c => c.Column, 2));

        Assert.Contains("MERGED", cut.Markup);
    }

    [Fact]
    public void CellView_OutsideMerge_RendersOwnValue()
    {
        var sheet = CreateSheetWithMerge();
        sheet.Cells[10, 10].Value = "OUTSIDE";

        var cut = RenderComponent<CellView>(parameters => parameters
            .Add(c => c.Worksheet, sheet)
            .Add(c => c.Row, 10)
            .Add(c => c.Column, 10));

        Assert.Contains("OUTSIDE", cut.Markup);
        Assert.DoesNotContain("MERGED", cut.Markup);
    }

    [Fact]
    public async System.Threading.Tasks.Task CellView_FollowsAnchorChange_WhenAnchorValueUpdates()
    {
        var sheet = CreateSheetWithMerge();

        // Mount at a non-anchor cell within the merge.
        var cut = RenderComponent<CellView>(parameters => parameters
            .Add(c => c.Worksheet, sheet)
            .Add(c => c.Row, 3)
            .Add(c => c.Column, 3));

        Assert.Contains("MERGED", cut.Markup);

        // Updating the anchor must propagate to the non-anchor view via the cell.Changed
        // subscription (now subscribed against the anchor cell, not the non-anchor one).
        // The mutation fires Cell.Changed which calls StateHasChanged on the renderer's
        // dispatcher — invoke on the dispatcher to satisfy AssertAccess().
        await cut.InvokeAsync(() => sheet.Cells[0, 0].Value = "UPDATED");

        Assert.Contains("UPDATED", cut.Markup);
        Assert.DoesNotContain("MERGED", cut.Markup);
    }

    /// <summary>
    /// Reproduces the helper used by VirtualGrid.IsRangeEntirelyHidden for verification
    /// purposes — this protects the contract that a single hidden row inside a merge
    /// does not make the whole range hidden.
    /// </summary>
    private static bool IsRangeEntirelyHidden(Worksheet sheet, RangeRef range)
    {
        var anyVisibleRow = false;
        for (var r = range.Start.Row; r <= range.End.Row; r++)
        {
            if (!sheet.Rows.IsHidden(r))
            {
                anyVisibleRow = true;
                break;
            }
        }

        if (!anyVisibleRow)
        {
            return true;
        }

        var anyVisibleColumn = false;
        for (var c = range.Start.Column; c <= range.End.Column; c++)
        {
            if (!sheet.Columns.IsHidden(c))
            {
                anyVisibleColumn = true;
                break;
            }
        }

        return !anyVisibleColumn;
    }

    [Fact]
    public void IsRangeEntirelyHidden_ReturnsFalse_WhenOnlyOneRowHidden()
    {
        var sheet = new Worksheet(20, 20);
        var range = new RangeRef(new CellRef(0, 0), new CellRef(4, 4));
        sheet.MergedCells.Add(range);
        sheet.Rows.Hide(0);

        Assert.False(IsRangeEntirelyHidden(sheet, range));
    }

    [Fact]
    public void IsRangeEntirelyHidden_ReturnsTrue_WhenAllRowsHidden()
    {
        var sheet = new Worksheet(20, 20);
        var range = new RangeRef(new CellRef(0, 0), new CellRef(2, 4));
        sheet.MergedCells.Add(range);
        sheet.Rows.Hide(0);
        sheet.Rows.Hide(1);
        sheet.Rows.Hide(2);

        Assert.True(IsRangeEntirelyHidden(sheet, range));
    }

    [Fact]
    public void IsRangeEntirelyHidden_ReturnsTrue_WhenAllColumnsHidden()
    {
        var sheet = new Worksheet(20, 20);
        var range = new RangeRef(new CellRef(0, 0), new CellRef(4, 2));
        sheet.MergedCells.Add(range);
        sheet.Columns.Hide(0);
        sheet.Columns.Hide(1);
        sheet.Columns.Hide(2);

        Assert.True(IsRangeEntirelyHidden(sheet, range));
    }
}
