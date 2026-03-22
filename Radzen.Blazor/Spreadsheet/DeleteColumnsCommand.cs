using System;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Command that deletes a contiguous range of columns and supports undo via snapshot.
/// </summary>
public class DeleteColumnsCommand : SheetSnapshotCommandBase
{
    private readonly int startColumnIndex;
    private readonly int endColumnIndex;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteColumnsCommand"/> class.
    /// </summary>
    public DeleteColumnsCommand(Worksheet sheet, int startColumnIndex, int endColumnIndex) : base(sheet)
    {
        if (startColumnIndex < 0 || endColumnIndex < startColumnIndex || endColumnIndex >= sheet.ColumnCount)
        {
            throw new ArgumentOutOfRangeException(nameof(startColumnIndex));
        }

        this.startColumnIndex = startColumnIndex;
        this.endColumnIndex = endColumnIndex;
    }

    /// <summary>
    /// Executes the command.
    /// </summary>
    protected override void DoExecute()
    {
        for (var c = endColumnIndex; c >= startColumnIndex; c--)
        {
            sheet.DeleteColumn(c);
        }
    }
}
