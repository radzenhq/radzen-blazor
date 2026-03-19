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
        RefreshCells();
        return true;
    }

    /// <inheritdoc/>
    public void Unexecute()
    {
        sheet.Validation.Remove(range, rule);
        RefreshCells();
    }

    private void RefreshCells()
    {
        foreach (var cellRef in range.GetCells())
        {
            if (sheet.Cells.TryGet(cellRef.Row, cellRef.Column, out var cell))
            {
                cell.Validate();
                cell.OnChanged();
            }
        }
    }
}
