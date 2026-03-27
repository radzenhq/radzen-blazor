using System;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Command that deletes a contiguous range of rows and supports undo via snapshot.
/// </summary>
public class DeleteRowsCommand : SheetSnapshotCommandBase, IProtectedCommand
{
    /// <inheritdoc/>
    public SheetAction RequiredAction => SheetAction.DeleteRows;

    private readonly int startRowIndex;
    private readonly int endRowIndex;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteRowsCommand"/> class.
    /// </summary>
    public DeleteRowsCommand(Worksheet sheet, int startRowIndex, int endRowIndex) : base(sheet)
    {
        if (startRowIndex < 0 || endRowIndex < startRowIndex || endRowIndex >= sheet.RowCount)
        {
            throw new ArgumentOutOfRangeException(nameof(startRowIndex));
        }

        this.startRowIndex = startRowIndex;
        this.endRowIndex = endRowIndex;
    }

    /// <summary>
    /// Executes the command.
    /// </summary>
    protected override void DoExecute()
    {
        for (var r = endRowIndex; r >= startRowIndex; r--)
        {
            sheet.DeleteRow(r);
        }
    }
}
