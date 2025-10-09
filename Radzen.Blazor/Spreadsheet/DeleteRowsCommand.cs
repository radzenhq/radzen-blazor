namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Command that deletes a contiguous range of rows and supports undo via snapshot.
/// </summary>
public class DeleteRowsCommand : RowCommandBase
{
    private readonly int startRowIndex;
    private readonly int endRowIndex;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteRowsCommand"/> class.
    /// </summary>
    /// <param name="sheet"></param>
    /// <param name="startRowIndex"></param>
    /// <param name="endRowIndex"></param>
    /// <exception cref="System.ArgumentOutOfRangeException"></exception>
    public DeleteRowsCommand(Sheet sheet, int startRowIndex, int endRowIndex) : base(sheet, startRowIndex)
    {
        if (startRowIndex < 0 || endRowIndex < startRowIndex || endRowIndex >= sheet.RowCount)
        {
            throw new System.ArgumentOutOfRangeException(nameof(startRowIndex));
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
