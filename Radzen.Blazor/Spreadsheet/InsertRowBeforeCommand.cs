namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Command that inserts a single row BEFORE the specified row index and supports undo via snapshot.
/// </summary>
public class InsertRowBeforeCommand(Sheet sheet, int rowIndex) : RowCommandBase(sheet, rowIndex)
{
    /// <summary>
    /// Executes the command.
    /// </summary>
    protected override void DoExecute() => sheet.InsertRow(rowIndex, 1);
}
