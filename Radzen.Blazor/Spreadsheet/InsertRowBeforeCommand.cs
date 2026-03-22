using System;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Command that inserts a single row BEFORE the specified row index and supports undo via snapshot.
/// </summary>
public class InsertRowBeforeCommand : SheetSnapshotCommandBase
{
    private readonly int rowIndex;

    /// <summary>
    /// Initializes a new instance of the <see cref="InsertRowBeforeCommand"/> class.
    /// </summary>
    public InsertRowBeforeCommand(Worksheet sheet, int rowIndex) : base(sheet)
    {
        if (rowIndex < 0 || rowIndex >= sheet.RowCount)
        {
            throw new ArgumentOutOfRangeException(nameof(rowIndex));
        }

        this.rowIndex = rowIndex;
    }

    /// <summary>
    /// Executes the command.
    /// </summary>
    protected override void DoExecute() => sheet.InsertRow(rowIndex, 1);
}
