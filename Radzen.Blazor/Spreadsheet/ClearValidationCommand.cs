using System.Collections.Generic;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Command to clear all data validation rules from a range.
/// </summary>
public class ClearValidationCommand(Worksheet sheet, RangeRef range) : ICommand
{
    private readonly Worksheet sheet = sheet;
    private readonly RangeRef range = range;
    private List<ICellValidator> savedValidators = [];

    /// <inheritdoc/>
    public bool Execute()
    {
        savedValidators = sheet.Validation.RemoveAll(range);
        sheet.RefreshCells(range, validate: true);
        return true;
    }

    /// <inheritdoc/>
    public void Unexecute()
    {
        foreach (var validator in savedValidators)
        {
            sheet.Validation.Add(range, validator);
        }
        sheet.RefreshCells(range, validate: true);
    }
}
