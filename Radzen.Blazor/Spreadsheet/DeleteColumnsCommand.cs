namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Command that deletes a contiguous range of columns and supports undo via snapshot.
/// </summary>
public class DeleteColumnsCommand : ColumnCommandBase
{
    private readonly int startColumnIndex;
    private readonly int endColumnIndex;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteColumnsCommand"/> class.
    /// </summary>
    /// <param name="sheet"></param>
    /// <param name="startColumnIndex"></param>
    /// <param name="endColumnIndex"></param>
    /// <exception cref="System.ArgumentOutOfRangeException"></exception>
    public DeleteColumnsCommand(Sheet sheet, int startColumnIndex, int endColumnIndex) : base(sheet, startColumnIndex)
    {
        if (startColumnIndex < 0 || endColumnIndex < startColumnIndex || endColumnIndex >= sheet.ColumnCount)
        {
            throw new System.ArgumentOutOfRangeException(nameof(startColumnIndex));
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