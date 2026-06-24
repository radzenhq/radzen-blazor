#nullable enable

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

class ClearContentsCommand : RangeSnapshotCommandBase
{
    public override SheetAction RequiredAction => SheetAction.EditCell;

    public override SpreadsheetFeature? Feature => SpreadsheetFeature.Editing;

    private readonly RangeRef range;

    public ClearContentsCommand(Worksheet sheet, RangeRef range)
        : base(sheet)
    {
        this.range = range;
    }

    protected override bool DoExecute()
    {
        for (var row = range.Start.Row; row <= range.End.Row; row++)
        {
            for (var column = range.Start.Column; column <= range.End.Column; column++)
            {
                if (sheet.Cells.TryGet(row, column, out var cell))
                {
                    Capture(new CellRef(row, column));

                    cell.Formula = null;
                    cell.Value = null;
                }
            }
        }

        return snapshot.Count > 0;
    }

    public override void Unexecute()
    {
        RestoreSnapshot();
    }
}
