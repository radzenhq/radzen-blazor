using System;

namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Represents a command to toggle filter button on a table.
/// </summary>
public class TableFilterCommand : ICommand
{
    private readonly Sheet sheet;
    private readonly int tableIndex;
    private readonly bool previousShowFilterButton;

    /// <summary>
    /// Initializes a new instance of the <see cref="TableFilterCommand"/> class.
    /// </summary>
    /// <param name="sheet">The sheet containing the table.</param>
    /// <param name="tableIndex">The index of the table to toggle filter button on.</param>
    public TableFilterCommand(Sheet sheet, int tableIndex)
    {
        this.sheet = sheet;
        this.tableIndex = tableIndex;
        this.previousShowFilterButton = tableIndex >= 0 && tableIndex < sheet.Tables.Count 
            ? sheet.Tables[tableIndex].ShowFilterButton 
            : false;
    }

    /// <inheritdoc/>
    public bool Execute()
    {
        if (tableIndex >= 0 && tableIndex < sheet.Tables.Count)
        {
            var table = sheet.Tables[tableIndex];
            table.ShowFilterButton = !table.ShowFilterButton;
        }
        return true;
    }

    /// <inheritdoc/>
    public void Unexecute()
    {
        if (tableIndex >= 0 && tableIndex < sheet.Tables.Count)
        {
            var table = sheet.Tables[tableIndex];
            table.ShowFilterButton = previousShowFilterButton;
        }
    }
} 