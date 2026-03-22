using System;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Command that inserts a single column BEFORE the specified index and supports undo via snapshot.
/// </summary>
public class InsertColumnBeforeCommand : SheetSnapshotCommandBase
{
    private readonly int columnIndex;

    /// <summary>
    /// Initializes a new instance of the <see cref="InsertColumnBeforeCommand"/> class.
    /// </summary>
    public InsertColumnBeforeCommand(Worksheet sheet, int columnIndex) : base(sheet)
    {
        if (columnIndex < 0 || columnIndex >= sheet.ColumnCount)
        {
            throw new ArgumentOutOfRangeException(nameof(columnIndex));
        }

        this.columnIndex = columnIndex;
    }

    /// <summary>
    /// Executes the command.
    /// </summary>
    protected override void DoExecute() => sheet.InsertColumn(columnIndex, 1);
}
