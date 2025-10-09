namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Command that deletes a single column and supports undo via snapshot.
/// </summary>
public class DeleteColumnCommand(Sheet sheet, int columnIndex) : ColumnCommandBase(sheet, columnIndex)
{
    /// <summary>
    /// Executes the command.
    /// </summary>
    protected override void DoExecute() => sheet.DeleteColumn(this.columnIndex);
}