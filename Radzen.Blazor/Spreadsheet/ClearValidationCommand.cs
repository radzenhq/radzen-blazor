using System.Collections.Generic;

namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Command to clear all data validation rules from a range.
/// </summary>
public class ClearValidationCommand(Sheet sheet, RangeRef range) : ICommand
{
    private readonly Sheet sheet = sheet;
    private readonly RangeRef range = range;
    private List<ICellValidator> savedValidators = [];

    /// <inheritdoc/>
    public bool Execute()
    {
        savedValidators = sheet.Validation.RemoveAll(range);
        RefreshCells();
        return true;
    }

    /// <inheritdoc/>
    public void Unexecute()
    {
        foreach (var validator in savedValidators)
        {
            sheet.Validation.Add(range, validator);
        }
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
