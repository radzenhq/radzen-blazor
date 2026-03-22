using System.Collections.Generic;
using System.Linq;

namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Represents a store for merged cell ranges in a spreadsheet.
/// </summary>
/// <param name="sheet"></param>
public class MergedCellStore(Sheet sheet)
{
    private readonly List<RangeRef> data = [];
    private readonly Dictionary<(int row, int column), RangeRef> index = [];
    private readonly Sheet sheet = sheet;

    /// <summary>
    /// Gets the list of merged cell ranges in the store.
    /// </summary>
    public IReadOnlyList<RangeRef> Ranges => data;

    /// <summary>
    /// Adds a merged cell range to the store.
    /// </summary>
    /// <param name="range"></param>
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
    /// <param name="range"></param>
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
    /// <param name="address"></param>
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

    /// <summary>
    /// Adjusts all merged ranges after a row is deleted.
    /// Ranges fully on the deleted row are removed. Ranges spanning the deleted row shrink.
    /// Ranges below the deleted row shift up by one.
    /// </summary>
    internal void ShiftRowsUp(int deletedRow)
    {
        for (int i = data.Count - 1; i >= 0; i--)
        {
            var range = data[i];

            if (range.Start.Row > deletedRow)
            {
                // Entirely below — shift up
                var shifted = new RangeRef(
                    new CellRef(range.Start.Row - 1, range.Start.Column),
                    new CellRef(range.End.Row - 1, range.End.Column));
                ReplaceAt(i, shifted);
            }
            else if (range.End.Row >= deletedRow && range.Start.Row <= deletedRow)
            {
                // Spans or sits on the deleted row
                if (range.Start.Row == range.End.Row)
                {
                    // Entirely on the deleted row — remove
                    UnindexRange(range);
                    data.RemoveAt(i);
                }
                else
                {
                    // Shrink: end row decreases by one
                    var shrunk = new RangeRef(
                        range.Start,
                        new CellRef(range.End.Row - 1, range.End.Column));
                    ReplaceAt(i, shrunk);
                }
            }
            // Entirely above — no change
        }
    }

    /// <summary>
    /// Adjusts all merged ranges after rows are inserted.
    /// Ranges at or below the insert point shift down by count.
    /// </summary>
    internal void ShiftRowsDown(int fromRow, int count)
    {
        for (int i = 0; i < data.Count; i++)
        {
            var range = data[i];

            if (range.Start.Row >= fromRow)
            {
                // Entirely at or below — shift down
                var shifted = new RangeRef(
                    new CellRef(range.Start.Row + count, range.Start.Column),
                    new CellRef(range.End.Row + count, range.End.Column));
                ReplaceAt(i, shifted);
            }
            else if (range.End.Row >= fromRow)
            {
                // Starts above, ends at or below — expand
                var expanded = new RangeRef(
                    range.Start,
                    new CellRef(range.End.Row + count, range.End.Column));
                ReplaceAt(i, expanded);
            }
            // Entirely above — no change
        }
    }

    /// <summary>
    /// Adjusts all merged ranges after a column is deleted.
    /// </summary>
    internal void ShiftColumnsLeft(int deletedColumn)
    {
        for (int i = data.Count - 1; i >= 0; i--)
        {
            var range = data[i];

            if (range.Start.Column > deletedColumn)
            {
                var shifted = new RangeRef(
                    new CellRef(range.Start.Row, range.Start.Column - 1),
                    new CellRef(range.End.Row, range.End.Column - 1));
                ReplaceAt(i, shifted);
            }
            else if (range.End.Column >= deletedColumn && range.Start.Column <= deletedColumn)
            {
                if (range.Start.Column == range.End.Column)
                {
                    UnindexRange(range);
                    data.RemoveAt(i);
                }
                else
                {
                    var shrunk = new RangeRef(
                        range.Start,
                        new CellRef(range.End.Row, range.End.Column - 1));
                    ReplaceAt(i, shrunk);
                }
            }
        }
    }

    /// <summary>
    /// Adjusts all merged ranges after columns are inserted.
    /// </summary>
    internal void ShiftColumnsRight(int fromColumn, int count)
    {
        for (int i = 0; i < data.Count; i++)
        {
            var range = data[i];

            if (range.Start.Column >= fromColumn)
            {
                var shifted = new RangeRef(
                    new CellRef(range.Start.Row, range.Start.Column + count),
                    new CellRef(range.End.Row, range.End.Column + count));
                ReplaceAt(i, shifted);
            }
            else if (range.End.Column >= fromColumn)
            {
                var expanded = new RangeRef(
                    range.Start,
                    new CellRef(range.End.Row, range.End.Column + count));
                ReplaceAt(i, expanded);
            }
        }
    }

    private void ReplaceAt(int i, RangeRef newRange)
    {
        UnindexRange(data[i]);
        data[i] = newRange;
        IndexRange(newRange);
    }

    internal List<RangeRef> SplitRange(RangeRef range)
    {
        var result = new List<RangeRef>();

        if (range.Start.Row < sheet.Rows.Frozen && range.End.Row >= sheet.Rows.Frozen)
        {
            if (range.Start.Column < sheet.Columns.Frozen && range.End.Column >= sheet.Columns.Frozen)
            {
                // Split into 4 regions
                result.Add(new RangeRef(range.Start, new CellRef(sheet.Rows.Frozen - 1, sheet.Columns.Frozen - 1)));
                result.Add(new RangeRef(new CellRef(sheet.Rows.Frozen, range.Start.Column), new CellRef(range.End.Row, sheet.Columns.Frozen - 1)));
                result.Add(new RangeRef(new CellRef(range.Start.Row, sheet.Columns.Frozen), new CellRef(sheet.Rows.Frozen - 1, range.End.Column)));
                result.Add(new RangeRef(new CellRef(sheet.Rows.Frozen, sheet.Columns.Frozen), range.End));
            }
            else
            {
                // Split into 2 regions vertically
                result.Add(new RangeRef(range.Start, new CellRef(sheet.Rows.Frozen - 1, range.End.Column)));
                result.Add(new RangeRef(new CellRef(sheet.Rows.Frozen, range.Start.Column), range.End));
            }
        }
        else if (range.Start.Column < sheet.Columns.Frozen && range.End.Column >= sheet.Columns.Frozen)
        {
            // Split into 2 regions horizontally
            result.Add(new RangeRef(range.Start, new CellRef(range.End.Row, sheet.Columns.Frozen - 1)));
            result.Add(new RangeRef(new CellRef(range.Start.Row, sheet.Columns.Frozen), range.End));
        }
        else
        {
            result.Add(range);
        }

        return result;
    }
}