using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Represents a command to remove a filter from a sheet, supporting undo and redo operations.
/// </summary>
public class RemoveFilterCommand(Worksheet sheet, SheetFilter filter) : ICommand
{
    private readonly Worksheet sheet = sheet;
    private readonly SheetFilter filter = filter;

    /// <inheritdoc/>
    public bool Execute()
    {
        return sheet.RemoveFilter(filter);
    }

    /// <inheritdoc/>
    public void Unexecute()
    {
        sheet.AddFilter(filter);
    }
}
