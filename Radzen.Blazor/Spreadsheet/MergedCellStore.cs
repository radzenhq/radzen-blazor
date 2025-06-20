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
        }
    }

    /// <summary>
    /// Checks if the store contains a merged range that includes the specified cell address.
    /// </summary>
    public bool Contains(CellRef address) => GetMergedRange(address) != RangeRef.Invalid;

    /// <summary>
    /// Checks if the store contains a specific range.
    /// </summary>
    public bool Contains(RangeRef range) => data.Contains(range);

    /// <summary>
    /// Gets the merged range that contains the specified cell address.
    /// </summary>
    /// <param name="address"></param>
    /// <returns>The merged range if found; otherwise, returns <see cref="RangeRef.Invalid"/>.</returns>
    public RangeRef GetMergedRange(CellRef address)
    {
        foreach (var range in data)
        {
            if (range.Contains(address))
            {
                return range;
            }
        }

        return RangeRef.Invalid;
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
    /// Gets a list of merged ranges that overlap with the specified range.
    /// </summary>
    public List<RangeRef> GetOverlappingRanges(RangeRef range) => [.. data.Where(range.Overlaps)];

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