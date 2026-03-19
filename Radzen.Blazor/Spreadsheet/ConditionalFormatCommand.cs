using System.Collections.Generic;

namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Command to add a conditional formatting rule to a range.
/// </summary>
public class ConditionalFormatCommand(Sheet sheet, RangeRef range, ConditionalFormatBase rule) : ICommand
{
    private readonly Sheet sheet = sheet;
    private readonly RangeRef range = range;
    private readonly ConditionalFormatBase rule = rule;

    /// <inheritdoc/>
    public bool Execute()
    {
        sheet.ConditionalFormats.Add(range, rule);
        RefreshCells();
        return true;
    }

    /// <inheritdoc/>
    public void Unexecute()
    {
        sheet.ConditionalFormats.Remove(range, rule);
        RefreshCells();
    }

    private void RefreshCells()
    {
        foreach (var cellRef in range.GetCells())
        {
            if (sheet.Cells.TryGet(cellRef.Row, cellRef.Column, out var cell))
            {
                cell.OnChanged();
            }
        }
    }
}
