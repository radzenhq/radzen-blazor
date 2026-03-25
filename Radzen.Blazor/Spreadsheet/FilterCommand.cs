using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Represents a command to add a filter to a sheet, supporting undo and redo operations.
/// </summary>
public class FilterCommand(Worksheet sheet, SheetFilter filter) : ICommand
{
    private readonly Worksheet sheet = sheet;
    private readonly SheetFilter filter = filter;

    /// <inheritdoc/>
    public bool Execute()
    {
        sheet.AddFilter(filter);
        return true;
    }

    /// <inheritdoc/>
    public void Unexecute()
    {
        sheet.RemoveFilter(filter);
    }
}
