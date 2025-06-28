using System;

namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Represents a command to add a filter to a sheet, supporting undo and redo operations.
/// </summary>
public class FilterCommand : ICommand
{
    private readonly Sheet sheet;
    private readonly SheetFilter filter;

    /// <summary>
    /// Initializes a new instance of the <see cref="FilterCommand"/> class.
    /// </summary>
    /// <param name="sheet">The sheet to add the filter to.</param>
    /// <param name="filter">The filter to add.</param>
    public FilterCommand(Sheet sheet, SheetFilter filter)
    {
        this.sheet = sheet ?? throw new ArgumentNullException(nameof(sheet));
        this.filter = filter ?? throw new ArgumentNullException(nameof(filter));
    }

    /// <inheritdoc/>
    public bool Execute()
    {
        sheet.AddFilter(filter);
        return true;
    }

    /// <inheritdoc/>
    public void Unexecute()
    {
        sheet.RemoveFilter(filter);
    }
} 