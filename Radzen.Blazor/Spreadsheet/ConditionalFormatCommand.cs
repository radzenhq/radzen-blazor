using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Command to add a conditional formatting rule to a range.
/// </summary>
public class ConditionalFormatCommand(Worksheet sheet, RangeRef range, ConditionalFormatBase rule) : ICommand, IProtectedCommand
{
    /// <inheritdoc/>
    public SheetAction RequiredAction => SheetAction.FormatCells;

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
