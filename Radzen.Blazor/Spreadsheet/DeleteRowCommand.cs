namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Command that deletes a single row and supports undo by snapshotting sheet state.
/// </summary>
/// <remarks>
/// Creates a new <see cref="DeleteRowCommand"/>.
/// </remarks>
/// <param name="sheet"></param>
/// <param name="rowIndex"></param>
public class DeleteRowCommand(Sheet sheet, int rowIndex) : RowCommandBase(sheet, rowIndex)
{
    /// <summary>
    /// Executes the command.
    /// </summary>
    /// <returns></returns>
    protected override void DoExecute() => sheet.DeleteRow(rowIndex);
}