using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Command to merge a range of cells.
/// </summary>
public class MergeCellsCommand : RangeSnapshotCommandBase
{
    /// <inheritdoc/>
    public override SheetAction RequiredAction => SheetAction.FormatCells;

    /// <inheritdoc/>
    public override SpreadsheetFeature? Feature => SpreadsheetFeature.Merging;

    private readonly RangeRef range;
    private readonly bool center;

    /// <summary>
    /// Initializes a new instance of the <see cref="MergeCellsCommand"/> class.
    /// </summary>
    public MergeCellsCommand(Worksheet sheet, RangeRef range, bool center = false)
        : base(sheet)
    {
        this.range = range;
        this.center = center;
    }

    /// <inheritdoc/>
    protected override bool DoExecute()
    {
        if (range.Start == range.End)
        {
            return false;
        }

        var overlapping = sheet.MergedCells.GetOverlappingRanges(range);
        if (overlapping.Count > 0)
        {
            return false;
        }

        foreach (var cellRef in range.GetCells())
        {
            Capture(cellRef);

            if (cellRef != range.Start)
            {
                var cell = sheet.Cells[cellRef.Row, cellRef.Column];
                cell.Value = null;
            }
        }

        sheet.MergedCells.Add(range);

        if (center)
        {
            var topLeft = sheet.Cells[range.Start.Row, range.Start.Column];
            topLeft.Format = topLeft.Format.WithTextAlign(TextAlign.Center);
        }

        return true;
    }

    /// <inheritdoc/>
    public override void Unexecute()
    {
        sheet.MergedCells.Remove(range);

        RestoreSnapshot();
    }
}
