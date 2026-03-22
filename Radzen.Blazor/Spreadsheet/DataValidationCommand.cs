using Radzen.Blazor.Spreadsheet;
namespace Radzen.Documents.Spreadsheet;

#nullable enable

/// <summary>
/// Command to add a data validation rule to a range.
/// </summary>
public class DataValidationCommand(Worksheet sheet, RangeRef range, ICellValidator rule) : ICommand
{
    private readonly Worksheet sheet = sheet;
    private readonly RangeRef range = range;
    private readonly ICellValidator rule = rule;

    /// <inheritdoc/>
    public bool Execute()
    {
        sheet.Validation.Add(range, rule);
        sheet.RefreshCells(range, validate: true);
        return true;
    }

    /// <inheritdoc/>
    public void Unexecute()
    {
        sheet.Validation.Remove(range, rule);
        sheet.RefreshCells(range, validate: true);
    }
}
