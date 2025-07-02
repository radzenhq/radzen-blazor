using System;

namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Represents a command to remove a filter from a sheet, supporting undo and redo operations.
/// </summary>
public class RemoveFilterCommand : ICommand
{
    private readonly Sheet sheet;
    private readonly SheetFilter filter;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoveFilterCommand"/> class.
    /// </summary>
    /// <param name="sheet">The sheet to remove the filter from.</param>
    /// <param name="filter">The filter to remove.</param>
    public RemoveFilterCommand(Sheet sheet, SheetFilter filter)
    {
        this.sheet = sheet ?? throw new ArgumentNullException(nameof(sheet));
        this.filter = filter ?? throw new ArgumentNullException(nameof(filter));
    }

    /// <inheritdoc/>
    public bool Execute()
    {
        return sheet.RemoveFilter(filter);
    }

    /// <inheritdoc/>
    public void Unexecute()
    {
        sheet.AddFilter(filter);
    }
} 