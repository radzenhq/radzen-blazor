using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Represents a command to sort a range in a spreadsheet, supporting undo and redo operations.
/// </summary>
public class SortCommand : RangeSnapshotCommandBase
{
    /// <summary>
    /// Convenience factory for the multi-key sort variant.
    /// </summary>
    public static MultiKeySortCommand MultiKey(Worksheet sheet, RangeRef range, SortKey[] keys, bool skipHeaderRow = false) =>
        new(sheet, range, keys, skipHeaderRow);

    /// <inheritdoc/>
    public override SheetAction RequiredAction => SheetAction.Sort;

    /// <inheritdoc/>
    public override SpreadsheetFeature? Feature => SpreadsheetFeature.Sorting;

    private readonly RangeRef range;
    private readonly SortOrder order;
    private readonly int keyIndex;
    private readonly bool skipHeaderRow;

    /// <summary>
    /// Initializes a new instance of the <see cref="SortCommand"/> class.
    /// </summary>
    /// <param name="sheet">The sheet containing the range to sort.</param>
    /// <param name="range">The range to sort.</param>
    /// <param name="order">The sort order (ascending or descending).</param>
    /// <param name="keyIndex">The column index to sort by.</param>
    /// <param name="skipHeaderRow">If true, skips the first row (header) when sorting.</param>
    public SortCommand(Worksheet sheet, RangeRef range, SortOrder order, int keyIndex, bool skipHeaderRow = false)
        : base(sheet)
    {
        this.range = range;
        this.order = order;
        this.keyIndex = keyIndex;
        this.skipHeaderRow = skipHeaderRow;
    }

    /// <inheritdoc/>
    protected override bool DoExecute()
    {
        if (range == RangeRef.Invalid)
        {
            return false;
        }

        var startRow = range.Start.Row + (skipHeaderRow ? 1 : 0);

        for (var row = startRow; row <= range.End.Row; row++)
        {
            for (var column = range.Start.Column; column <= range.End.Column; column++)
            {
                Capture(new CellRef(row, column));
            }
        }

        var sortRange = new RangeRef(new CellRef(startRow, range.Start.Column), range.End);
        sheet.Sort(sortRange, order, keyIndex);

        return true;
    }

    /// <inheritdoc/>
    public override void Unexecute()
    {
        if (range == RangeRef.Invalid)
        {
            return;
        }

        RestoreSnapshot();

        // RestoreSnapshot only restores cell content, not row visibility - re-apply filters by value.
        sheet.ReapplyFilters();
    }
}
