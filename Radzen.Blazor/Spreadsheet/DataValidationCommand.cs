namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Command to add a data validation rule to a range.
/// </summary>
public class DataValidationCommand(Sheet sheet, RangeRef range, ICellValidator rule) : ICommand
{
    private readonly Sheet sheet = sheet;
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
