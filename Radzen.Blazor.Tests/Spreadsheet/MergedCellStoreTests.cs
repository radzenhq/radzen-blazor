using System.Linq;
using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class MergedCellStoreTests
{
    private readonly Worksheet sheet = new(20, 20);

    // --- Add ---

    [Fact]
    public void Add_StoresRange()
    {
        var range = new RangeRef(new CellRef(0, 0), new CellRef(2, 2));
        sheet.MergedCells.Add(range);

        Assert.Single(sheet.MergedCells.Ranges);
        Assert.Equal(range, sheet.MergedCells.Ranges[0]);
    }

    [Fact]
    public void Add_IgnoresInvalidRange()
    {
        sheet.MergedCells.Add(RangeRef.Invalid);

        Assert.Empty(sheet.MergedCells.Ranges);
    }

    [Fact]
    public void Add_MultipleRanges()
    {
        var r1 = new RangeRef(new CellRef(0, 0), new CellRef(1, 1));
        var r2 = new RangeRef(new CellRef(5, 5), new CellRef(7, 7));
        sheet.MergedCells.Add(r1);
        sheet.MergedCells.Add(r2);

        Assert.Equal(2, sheet.MergedCells.Ranges.Count);
    }

    // --- Remove ---

    [Fact]
    public void Remove_ReturnsTrue_WhenRangeExists()
    {
        var range = new RangeRef(new CellRef(0, 0), new CellRef(2, 2));
        sheet.MergedCells.Add(range);

        Assert.True(sheet.MergedCells.Remove(range));
        Assert.Empty(sheet.MergedCells.Ranges);
    }

    [Fact]
    public void Remove_ReturnsFalse_WhenRangeDoesNotExist()
    {
        var range = new RangeRef(new CellRef(0, 0), new CellRef(2, 2));

        Assert.False(sheet.MergedCells.Remove(range));
    }

    [Fact]
    public void Remove_ClearsIndex()
    {
        var range = new RangeRef(new CellRef(0, 0), new CellRef(1, 1));
        sheet.MergedCells.Add(range);
        sheet.MergedCells.Remove(range);

        Assert.False(sheet.MergedCells.Contains(new CellRef(0, 0)));
        Assert.False(sheet.MergedCells.Contains(new CellRef(0, 1)));
        Assert.False(sheet.MergedCells.Contains(new CellRef(1, 0)));
        Assert.False(sheet.MergedCells.Contains(new CellRef(1, 1)));
    }

    // --- Contains(CellRef) ---

    [Fact]
    public void Contains_CellRef_ReturnsTrueForAllCellsInRange()
    {
        var range = new RangeRef(new CellRef(2, 3), new CellRef(4, 5));
        sheet.MergedCells.Add(range);

        for (int row = 2; row <= 4; row++)
        {
            for (int col = 3; col <= 5; col++)
            {
                Assert.True(sheet.MergedCells.Contains(new CellRef(row, col)),
                    $"Expected ({row},{col}) to be contained");
            }
        }
    }

    [Fact]
    public void Contains_CellRef_ReturnsFalseForCellsOutsideRange()
    {
        var range = new RangeRef(new CellRef(2, 3), new CellRef(4, 5));
        sheet.MergedCells.Add(range);

        Assert.False(sheet.MergedCells.Contains(new CellRef(1, 3)));
        Assert.False(sheet.MergedCells.Contains(new CellRef(5, 3)));
        Assert.False(sheet.MergedCells.Contains(new CellRef(2, 2)));
        Assert.False(sheet.MergedCells.Contains(new CellRef(2, 6)));
        Assert.False(sheet.MergedCells.Contains(new CellRef(0, 0)));
    }

    [Fact]
    public void Contains_CellRef_ReturnsFalseWhenEmpty()
    {
        Assert.False(sheet.MergedCells.Contains(new CellRef(0, 0)));
    }

    // --- Contains(RangeRef) ---

    [Fact]
    public void Contains_RangeRef_ReturnsTrueForExactMatch()
    {
        var range = new RangeRef(new CellRef(0, 0), new CellRef(2, 2));
        sheet.MergedCells.Add(range);

        Assert.True(sheet.MergedCells.Contains(range));
    }

    [Fact]
    public void Contains_RangeRef_ReturnsFalseForDifferentRange()
    {
        var range = new RangeRef(new CellRef(0, 0), new CellRef(2, 2));
        sheet.MergedCells.Add(range);

        Assert.False(sheet.MergedCells.Contains(new RangeRef(new CellRef(0, 0), new CellRef(1, 1))));
    }

    // --- GetMergedRange ---

    [Fact]
    public void GetMergedRange_ReturnsRange_ForCellInsideRange()
    {
        var range = new RangeRef(new CellRef(1, 1), new CellRef(3, 3));
        sheet.MergedCells.Add(range);

        Assert.Equal(range, sheet.MergedCells.GetMergedRange(new CellRef(2, 2)));
    }

    [Fact]
    public void GetMergedRange_ReturnsRange_ForStartCell()
    {
        var range = new RangeRef(new CellRef(1, 1), new CellRef(3, 3));
        sheet.MergedCells.Add(range);

        Assert.Equal(range, sheet.MergedCells.GetMergedRange(new CellRef(1, 1)));
    }

    [Fact]
    public void GetMergedRange_ReturnsRange_ForEndCell()
    {
        var range = new RangeRef(new CellRef(1, 1), new CellRef(3, 3));
        sheet.MergedCells.Add(range);

        Assert.Equal(range, sheet.MergedCells.GetMergedRange(new CellRef(3, 3)));
    }

    [Fact]
    public void GetMergedRange_ReturnsInvalid_ForCellOutsideRange()
    {
        var range = new RangeRef(new CellRef(1, 1), new CellRef(3, 3));
        sheet.MergedCells.Add(range);

        Assert.Equal(RangeRef.Invalid, sheet.MergedCells.GetMergedRange(new CellRef(0, 0)));
    }

    [Fact]
    public void GetMergedRange_ReturnsInvalid_WhenEmpty()
    {
        Assert.Equal(RangeRef.Invalid, sheet.MergedCells.GetMergedRange(new CellRef(5, 5)));
    }

    [Fact]
    public void GetMergedRange_ReturnsCorrectRange_WithMultipleRanges()
    {
        var r1 = new RangeRef(new CellRef(0, 0), new CellRef(1, 1));
        var r2 = new RangeRef(new CellRef(5, 5), new CellRef(7, 7));
        sheet.MergedCells.Add(r1);
        sheet.MergedCells.Add(r2);

        Assert.Equal(r1, sheet.MergedCells.GetMergedRange(new CellRef(0, 0)));
        Assert.Equal(r1, sheet.MergedCells.GetMergedRange(new CellRef(1, 1)));
        Assert.Equal(r2, sheet.MergedCells.GetMergedRange(new CellRef(6, 6)));
        Assert.Equal(r2, sheet.MergedCells.GetMergedRange(new CellRef(5, 5)));
        Assert.Equal(RangeRef.Invalid, sheet.MergedCells.GetMergedRange(new CellRef(3, 3)));
    }

    [Fact]
    public void GetMergedRange_SingleCellRange()
    {
        // A single cell can't really be merged, but test the edge case
        var range = new RangeRef(new CellRef(5, 5), new CellRef(5, 5));
        sheet.MergedCells.Add(range);

        Assert.Equal(range, sheet.MergedCells.GetMergedRange(new CellRef(5, 5)));
        Assert.Equal(RangeRef.Invalid, sheet.MergedCells.GetMergedRange(new CellRef(5, 6)));
    }

    // --- GetMergedRangeOrSelf ---

    [Fact]
    public void GetMergedRangeOrSelf_ReturnsMergedRange_WhenMerged()
    {
        var range = new RangeRef(new CellRef(1, 1), new CellRef(3, 3));
        sheet.MergedCells.Add(range);

        Assert.Equal(range, sheet.MergedCells.GetMergedRangeOrSelf(new CellRef(2, 2)));
    }

    [Fact]
    public void GetMergedRangeOrSelf_ReturnsSelfRange_WhenNotMerged()
    {
        var cell = new CellRef(5, 5);
        var result = sheet.MergedCells.GetMergedRangeOrSelf(cell);

        Assert.Equal(cell.ToRange(), result);
    }

    // --- GetMergedRangeStartOrSelf ---

    [Fact]
    public void GetMergedRangeStartOrSelf_ReturnsMergeStart_WhenMerged()
    {
        var range = new RangeRef(new CellRef(1, 1), new CellRef(3, 3));
        sheet.MergedCells.Add(range);

        Assert.Equal(new CellRef(1, 1), sheet.MergedCells.GetMergedRangeStartOrSelf(new CellRef(2, 2)));
    }

    [Fact]
    public void GetMergedRangeStartOrSelf_ReturnsSelf_WhenNotMerged()
    {
        var cell = new CellRef(5, 5);
        Assert.Equal(cell, sheet.MergedCells.GetMergedRangeStartOrSelf(cell));
    }

    // --- GetOverlappingRanges ---

    [Fact]
    public void GetOverlappingRanges_FindsOverlappingRanges()
    {
        var r1 = new RangeRef(new CellRef(0, 0), new CellRef(2, 2));
        var r2 = new RangeRef(new CellRef(5, 5), new CellRef(7, 7));
        sheet.MergedCells.Add(r1);
        sheet.MergedCells.Add(r2);

        var query = new RangeRef(new CellRef(1, 1), new CellRef(6, 6));
        var result = sheet.MergedCells.GetOverlappingRanges(query);

        Assert.Equal(2, result.Count);
        Assert.Contains(r1, result);
        Assert.Contains(r2, result);
    }

    [Fact]
    public void GetOverlappingRanges_ReturnsEmpty_WhenNoOverlap()
    {
        var range = new RangeRef(new CellRef(0, 0), new CellRef(2, 2));
        sheet.MergedCells.Add(range);

        var query = new RangeRef(new CellRef(5, 5), new CellRef(7, 7));
        Assert.Empty(sheet.MergedCells.GetOverlappingRanges(query));
    }

    [Fact]
    public void GetOverlappingRanges_FindsPartialOverlap()
    {
        var range = new RangeRef(new CellRef(2, 2), new CellRef(5, 5));
        sheet.MergedCells.Add(range);

        var query = new RangeRef(new CellRef(4, 4), new CellRef(8, 8));
        var result = sheet.MergedCells.GetOverlappingRanges(query);

        Assert.Single(result);
        Assert.Equal(range, result[0]);
    }

    // --- Index consistency after Add/Remove sequences ---

    [Fact]
    public void AddRemoveAdd_IndexRemainsConsistent()
    {
        var range = new RangeRef(new CellRef(0, 0), new CellRef(1, 1));
        sheet.MergedCells.Add(range);
        sheet.MergedCells.Remove(range);

        Assert.False(sheet.MergedCells.Contains(new CellRef(0, 0)));

        // Re-add a different range covering the same cells
        var range2 = new RangeRef(new CellRef(0, 0), new CellRef(2, 2));
        sheet.MergedCells.Add(range2);

        Assert.Equal(range2, sheet.MergedCells.GetMergedRange(new CellRef(0, 0)));
        Assert.Equal(range2, sheet.MergedCells.GetMergedRange(new CellRef(2, 2)));
    }

    [Fact]
    public void RemoveFirst_LeavesSecondIntact()
    {
        var r1 = new RangeRef(new CellRef(0, 0), new CellRef(1, 1));
        var r2 = new RangeRef(new CellRef(5, 5), new CellRef(6, 6));
        sheet.MergedCells.Add(r1);
        sheet.MergedCells.Add(r2);

        sheet.MergedCells.Remove(r1);

        Assert.False(sheet.MergedCells.Contains(new CellRef(0, 0)));
        Assert.True(sheet.MergedCells.Contains(new CellRef(5, 5)));
        Assert.Equal(r2, sheet.MergedCells.GetMergedRange(new CellRef(6, 6)));
    }

    [Fact]
    public void RemoveSecond_LeavesFirstIntact()
    {
        var r1 = new RangeRef(new CellRef(0, 0), new CellRef(1, 1));
        var r2 = new RangeRef(new CellRef(5, 5), new CellRef(6, 6));
        sheet.MergedCells.Add(r1);
        sheet.MergedCells.Add(r2);

        sheet.MergedCells.Remove(r2);

        Assert.True(sheet.MergedCells.Contains(new CellRef(0, 0)));
        Assert.False(sheet.MergedCells.Contains(new CellRef(5, 5)));
        Assert.Equal(r1, sheet.MergedCells.GetMergedRange(new CellRef(1, 1)));
    }

    // --- Large range ---

    [Fact]
    public void LargeRange_AllCellsIndexed()
    {
        var largeSheet = new Worksheet(100, 100);
        var range = new RangeRef(new CellRef(0, 0), new CellRef(49, 49));
        largeSheet.MergedCells.Add(range);

        // Spot-check corners and middle
        Assert.Equal(range, largeSheet.MergedCells.GetMergedRange(new CellRef(0, 0)));
        Assert.Equal(range, largeSheet.MergedCells.GetMergedRange(new CellRef(49, 49)));
        Assert.Equal(range, largeSheet.MergedCells.GetMergedRange(new CellRef(25, 25)));
        Assert.Equal(RangeRef.Invalid, largeSheet.MergedCells.GetMergedRange(new CellRef(50, 50)));
    }

    // --- Wide range (single row, many columns) ---

    [Fact]
    public void WideRange_AllCellsIndexed()
    {
        var range = new RangeRef(new CellRef(0, 0), new CellRef(0, 19));
        sheet.MergedCells.Add(range);

        for (int col = 0; col < 20; col++)
        {
            Assert.Equal(range, sheet.MergedCells.GetMergedRange(new CellRef(0, col)));
        }

        Assert.Equal(RangeRef.Invalid, sheet.MergedCells.GetMergedRange(new CellRef(1, 0)));
    }

    // --- Tall range (many rows, single column) ---

    [Fact]
    public void TallRange_AllCellsIndexed()
    {
        var range = new RangeRef(new CellRef(0, 0), new CellRef(19, 0));
        sheet.MergedCells.Add(range);

        for (int row = 0; row < 20; row++)
        {
            Assert.Equal(range, sheet.MergedCells.GetMergedRange(new CellRef(row, 0)));
        }

        Assert.Equal(RangeRef.Invalid, sheet.MergedCells.GetMergedRange(new CellRef(0, 1)));
    }

    // --- Many small ranges ---

    [Fact]
    public void ManySmallRanges_EachIndexedCorrectly()
    {
        var ranges = new RangeRef[10];
        for (int i = 0; i < 10; i++)
        {
            ranges[i] = new RangeRef(new CellRef(i * 2, 0), new CellRef(i * 2 + 1, 1));
            sheet.MergedCells.Add(ranges[i]);
        }

        for (int i = 0; i < 10; i++)
        {
            Assert.Equal(ranges[i], sheet.MergedCells.GetMergedRange(new CellRef(i * 2, 0)));
            Assert.Equal(ranges[i], sheet.MergedCells.GetMergedRange(new CellRef(i * 2 + 1, 1)));
        }
    }

    // --- Adjacent ranges ---

    [Fact]
    public void AdjacentRanges_NoIndexCollision()
    {
        var r1 = new RangeRef(new CellRef(0, 0), new CellRef(0, 2));
        var r2 = new RangeRef(new CellRef(0, 3), new CellRef(0, 5));
        sheet.MergedCells.Add(r1);
        sheet.MergedCells.Add(r2);

        Assert.Equal(r1, sheet.MergedCells.GetMergedRange(new CellRef(0, 2)));
        Assert.Equal(r2, sheet.MergedCells.GetMergedRange(new CellRef(0, 3)));
    }

    // --- Integration with commands ---

    [Fact]
    public void MergeCellsCommand_UsesIndexCorrectly()
    {
        var view = new SheetView(sheet);
        sheet.Cells[0, 0].Value = "merged";
        var range = new RangeRef(new CellRef(0, 0), new CellRef(1, 1));

        var cmd = new MergeCellsCommand(sheet, range);
        view.Commands.Execute(cmd);

        Assert.True(sheet.MergedCells.Contains(new CellRef(0, 0)));
        Assert.True(sheet.MergedCells.Contains(new CellRef(1, 1)));

        view.Commands.Undo();

        Assert.False(sheet.MergedCells.Contains(new CellRef(0, 0)));
        Assert.False(sheet.MergedCells.Contains(new CellRef(1, 1)));
    }

    [Fact]
    public void UnmergeCellsCommand_ClearsIndex()
    {
        var view = new SheetView(sheet);
        var range = new RangeRef(new CellRef(0, 0), new CellRef(1, 1));
        sheet.MergedCells.Add(range);

        var cmd = new UnmergeCellsCommand(sheet, new CellRef(0, 0));
        view.Commands.Execute(cmd);

        Assert.False(sheet.MergedCells.Contains(new CellRef(0, 0)));

        view.Commands.Undo();

        Assert.True(sheet.MergedCells.Contains(new CellRef(0, 0)));
    }

    // --- Ranges property reflects state ---

    [Fact]
    public void Ranges_ReflectsAdditions()
    {
        Assert.Empty(sheet.MergedCells.Ranges);

        var range = new RangeRef(new CellRef(0, 0), new CellRef(1, 1));
        sheet.MergedCells.Add(range);

        Assert.Single(sheet.MergedCells.Ranges);
    }

    [Fact]
    public void Ranges_ReflectsRemovals()
    {
        var range = new RangeRef(new CellRef(0, 0), new CellRef(1, 1));
        sheet.MergedCells.Add(range);
        sheet.MergedCells.Remove(range);

        Assert.Empty(sheet.MergedCells.Ranges);
    }
}
