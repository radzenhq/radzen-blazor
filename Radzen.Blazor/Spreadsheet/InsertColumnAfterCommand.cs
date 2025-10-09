namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Command that inserts a single column AFTER the specified index and supports undo via snapshot.
/// </summary>
public class InsertColumnAfterCommand(Sheet sheet, int columnIndex) : ColumnCommandBase(sheet, columnIndex)
{
    /// <summary>
    /// Executes the command.
    /// </summary>
    protected override void DoExecute() => sheet.InsertColumn(this.columnIndex + 1, 1);
}
