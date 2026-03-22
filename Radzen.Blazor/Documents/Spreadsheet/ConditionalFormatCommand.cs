using Radzen.Blazor.Spreadsheet;
namespace Radzen.Documents.Spreadsheet;

#nullable enable

/// <summary>
/// Command to add a conditional formatting rule to a range.
/// </summary>
public class ConditionalFormatCommand(Worksheet sheet, RangeRef range, ConditionalFormatBase rule) : ICommand
{
    private readonly Worksheet sheet = sheet;
    private readonly RangeRef range = range;
    private readonly ConditionalFormatBase rule = rule;

    /// <inheritdoc/>
    public bool Execute()
    {
        sheet.ConditionalFormats.Add(range, rule);
        sheet.RefreshCells(range);
        return true;
    }

    /// <inheritdoc/>
    public void Unexecute()
    {
        sheet.ConditionalFormats.Remove(range, rule);
        sheet.RefreshCells(range);
    }
}
