using System;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Represents a command to toggle auto filter on a sheet.
/// </summary>
public class SheetAutoFilterCommand : ICommand, IProtectedCommand
{
    /// <inheritdoc/>
    public SheetAction RequiredAction => SheetAction.AutoFilter;

    /// <inheritdoc/>
    public SpreadsheetFeature? Feature => SpreadsheetFeature.Filtering;

    private readonly Worksheet sheet;
    private readonly RangeRef range;
    private RangeRef? previousRange;

    /// <summary>
    /// Initializes a new instance of the <see cref="SheetAutoFilterCommand"/> class.
    /// </summary>
    /// <param name="sheet">The sheet to toggle auto filter on.</param>
    /// <param name="range">The range to apply auto filter to.</param>
    public SheetAutoFilterCommand(Worksheet sheet, RangeRef range)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        this.sheet = sheet;
        this.range = range;
    }

    /// <inheritdoc/>
    public bool Execute()
    {
        previousRange = sheet.AutoFilter.Range;

        if (sheet.AutoFilter.Range is null)
        {
            sheet.AutoFilter.Range = range;
        }
        else
        {
            sheet.AutoFilter.Range = null;
        }

        sheet.OnChromeChanged();

        return true;
    }

    /// <inheritdoc/>
    public void Unexecute()
    {
        sheet.AutoFilter.Range = previousRange;
        sheet.OnChromeChanged();
    }
}
