using System.Collections.Generic;
using System.Linq;

namespace Radzen.Documents.Spreadsheet;

/// <summary>
/// Represents a store for merged cell ranges in a spreadsheet.
/// </summary>
public class MergedCellStore
{
    private readonly List<RangeRef> data = [];
    private readonly Dictionary<(int row, int column), RangeRef> index = [];

    /// <summary>
    /// Gets the list of merged cell ranges in the store.
    /// </summary>
    public IReadOnlyList<RangeRef> Ranges => data;

    /// <summary>
    /// Adds a merged cell range to the store.
    /// </summary>
    public void Add(RangeRef range)
    {
        if (range != RangeRef.Invalid)
        {
            data.Add(range);
            IndexRange(range);
        }
    }

    /// <summary>
    /// Removes a merged cell range from the store.
    /// </summary>
    public bool Remove(RangeRef range)
    {
        if (data.Remove(range))
        {
            UnindexRange(range);
            return true;
        }

        return false;
    }

    private void IndexRange(RangeRef range)
    {
        for (var row = range.Start.Row; row <= range.End.Row; row++)
        {
            for (var col = range.Start.Column; col <= range.End.Column; col++)
            {
                index[(row, col)] = range;
            }
        }
    }

    private void UnindexRange(RangeRef range)
    {
        for (var row = range.Start.Row; row <= range.End.Row; row++)
        {
            for (var col = range.Start.Column; col <= range.End.Column; col++)
            {
                index.Remove((row, col));
            }
        }
    }

    /// <summary>
    /// Checks if the store contains a merged range that includes the specified cell address.
    /// </summary>
    public bool Contains(CellRef address) => index.ContainsKey((address.Row, address.Column));

    /// <summary>
    /// Checks if the store contains a specific range.
    /// </summary>
    public bool Contains(RangeRef range) => data.Contains(range);

    /// <summary>
    /// Gets the merged range that contains the specified cell address.
    /// O(1) lookup via spatial index.
    /// </summary>
    /// <returns>The merged range if found; otherwise, returns <see cref="RangeRef.Invalid"/>.</returns>
    public RangeRef GetMergedRange(CellRef address)
    {
        return index.TryGetValue((address.Row, address.Column), out var range) ? range : RangeRef.Invalid;
    }

    /// <summary>
    /// Gets the merged range for the specified cell address, or returns the address as a range if no merged range is found.
    /// </summary>
    public RangeRef GetMergedRangeOrSelf(CellRef address)
    {
        var range = GetMergedRange(address);
        return range == RangeRef.Invalid ? address.ToRange() : range;
    }

    /// <summary>
    /// Gets the start cell of the merged range that contains the specified cell address, or returns the address itself if no merged range is found.
    /// </summary>
    public CellRef GetMergedRangeStartOrSelf(CellRef address) => GetMergedRangeOrSelf(address).Start;

    /// <summary>
    /// Gets a list of merged ranges that overlap with the specified range.
    /// </summary>
    public List<RangeRef> GetOverlappingRanges(RangeRef range) => [.. data.Where(range.Overlaps)];

    internal void ShiftRowsUp(int deletedRow) =>
        ShiftAxisOnDelete(deletedRow, isRow: true);

    internal void ShiftRowsDown(int fromRow, int count) =>
        ShiftAxisOnInsert(fromRow, count, isRow: true);

    internal void ShiftColumnsLeft(int deletedColumn) =>
        ShiftAxisOnDelete(deletedColumn, isRow: false);

    internal void ShiftColumnsRight(int fromColumn, int count) =>
        ShiftAxisOnInsert(fromColumn, count, isRow: false);

    private void ShiftAxisOnDelete(int deletedIndex, bool isRow)
    {
        for (int i = data.Count - 1; i >= 0; i--)
        {
            var range = data[i];
            var start = isRow ? range.Start.Row : range.Start.Column;
            var end = isRow ? range.End.Row : range.End.Column;

            if (start > deletedIndex)
            {
                // Entirely past the deleted index — shift toward it by one
                ReplaceAt(i, Translate(range, isRow, -1, -1));
            }
            else if (end >= deletedIndex && start <= deletedIndex)
            {
                // Spans or sits on the deleted index
                if (start == end)
                {
                    // Entirely on the deleted index — remove
                    UnindexRange(range);
                    data.RemoveAt(i);
                }
                else
                {
                    // Shrink: end decreases by one
                    ReplaceAt(i, Translate(range, isRow, 0, -1));
                }
            }
            // Entirely before — no change
        }
    }

    private void ShiftAxisOnInsert(int fromIndex, int count, bool isRow)
    {
        for (int i = 0; i < data.Count; i++)
        {
            var range = data[i];
            var start = isRow ? range.Start.Row : range.Start.Column;
            var end = isRow ? range.End.Row : range.End.Column;

            if (start >= fromIndex)
            {
                // Entirely at or after — shift both endpoints
                ReplaceAt(i, Translate(range, isRow, count, count));
            }
            else if (end >= fromIndex)
            {
                // Starts before, ends at or after — expand
                ReplaceAt(i, Translate(range, isRow, 0, count));
            }
            // Entirely before — no change
        }
    }

    private static RangeRef Translate(RangeRef range, bool isRow, int startDelta, int endDelta)
    {
        if (isRow)
        {
            return new RangeRef(
                new CellRef(range.Start.Row + startDelta, range.Start.Column),
                new CellRef(range.End.Row + endDelta, range.End.Column));
        }

        return new RangeRef(
            new CellRef(range.Start.Row, range.Start.Column + startDelta),
            new CellRef(range.End.Row, range.End.Column + endDelta));
    }

    private void ReplaceAt(int i, RangeRef newRange)
    {
        UnindexRange(data[i]);
        data[i] = newRange;
        IndexRange(newRange);
    }

    internal List<RangeRef> SplitRange(RangeRef range, int frozenRows, int frozenColumns)
    {
        var crossesRow = range.Start.Row < frozenRows && range.End.Row >= frozenRows;
        var crossesColumn = range.Start.Column < frozenColumns && range.End.Column >= frozenColumns;

        if (crossesRow && crossesColumn)
        {
            return
            [
                new(range.Start, new CellRef(frozenRows - 1, frozenColumns - 1)),
                new(new CellRef(frozenRows, range.Start.Column), new CellRef(range.End.Row, frozenColumns - 1)),
                new(new CellRef(range.Start.Row, frozenColumns), new CellRef(frozenRows - 1, range.End.Column)),
                new(new CellRef(frozenRows, frozenColumns), range.End),
            ];
        }
        if (crossesRow)
        {
            return
            [
                new(range.Start, new CellRef(frozenRows - 1, range.End.Column)),
                new(new CellRef(frozenRows, range.Start.Column), range.End),
            ];
        }
        if (crossesColumn)
        {
            return
            [
                new(range.Start, new CellRef(range.End.Row, frozenColumns - 1)),
                new(new CellRef(range.Start.Row, frozenColumns), range.End),
            ];
        }
        return [range];
    }
}