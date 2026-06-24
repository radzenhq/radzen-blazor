using System;
using Radzen.Documents.Spreadsheet;

namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Sort a range using one or more <see cref="SortKey"/> levels. Supports undo/redo by
/// snapshotting affected cells before sorting.
/// </summary>
public class MultiKeySortCommand : RangeSnapshotCommandBase
{
    /// <inheritdoc/>
    public override SheetAction RequiredAction => SheetAction.Sort;

    /// <inheritdoc/>
    public override SpreadsheetFeature? Feature => SpreadsheetFeature.Sorting;

    private readonly RangeRef range;
    private readonly SortKey[] keys;
    private readonly bool skipHeaderRow;

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiKeySortCommand"/> class.
    /// </summary>
    public MultiKeySortCommand(Worksheet sheet, RangeRef range, SortKey[] keys, bool skipHeaderRow = false)
        : base(sheet)
    {
        this.range = range;
        this.keys = keys ?? throw new ArgumentNullException(nameof(keys));
        this.skipHeaderRow = skipHeaderRow;
    }

    /// <inheritdoc/>
    protected override bool DoExecute()
    {
        if (range == RangeRef.Invalid || keys.Length == 0)
        {
            return false;
        }

        var startRow = range.Start.Row + (skipHeaderRow ? 1 : 0);

        for (var row = startRow; row <= range.End.Row; row++)
        {
            for (var col = range.Start.Column; col <= range.End.Column; col++)
            {
                Capture(new CellRef(row, col));
            }
        }

        var sortRange = new RangeRef(new CellRef(startRow, range.Start.Column), range.End);
        sheet.Sort(sortRange, keys);

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
