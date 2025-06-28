using System;

namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Represents a command to toggle auto filter on a sheet.
/// </summary>
public class SheetAutoFilterCommand : ICommand
{
    private readonly Sheet sheet;
    private readonly RangeRef range;
    private readonly AutoFilter? previousAutoFilter;

    /// <summary>
    /// Initializes a new instance of the <see cref="SheetAutoFilterCommand"/> class.
    /// </summary>
    /// <param name="sheet">The sheet to toggle auto filter on.</param>
    /// <param name="range">The range to apply auto filter to.</param>
    public SheetAutoFilterCommand(Sheet sheet, RangeRef range)
    {
        this.sheet = sheet;
        this.range = range;
        this.previousAutoFilter = sheet.AutoFilter;
    }

    /// <inheritdoc/>
    public bool Execute()
    {
        if (sheet.AutoFilter == null)
        {
            sheet.AutoFilter = new AutoFilter(sheet, range);
        }
        else
        {
            sheet.AutoFilter = null;
        }

        return true;
    }

    /// <inheritdoc/>
    public void Unexecute()
    {
        sheet.AutoFilter = previousAutoFilter;
    }
} 