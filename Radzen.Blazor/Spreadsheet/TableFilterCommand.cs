using System;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Represents a command to toggle filter button on a table.
/// </summary>
public class TableFilterCommand : ICommand, IProtectedCommand
{
    /// <inheritdoc/>
    public SheetAction RequiredAction => SheetAction.AutoFilter;

    /// <inheritdoc/>
    public SpreadsheetFeature? Feature => SpreadsheetFeature.Filtering;

    private readonly Worksheet sheet;
    private readonly Table? table;
    private bool previousShowFilterButton;

    /// <summary>
    /// Initializes a new instance of the <see cref="TableFilterCommand"/> class.
    /// </summary>
    /// <param name="sheet">The sheet containing the table.</param>
    /// <param name="table">The table to toggle filter button on.</param>
    public TableFilterCommand(Worksheet sheet, Table table)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        this.sheet = sheet;
        this.table = table;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TableFilterCommand"/> class.
    /// </summary>
    /// <param name="sheet">The sheet containing the table.</param>
    /// <param name="tableIndex">The index of the table to toggle filter button on.</param>
    public TableFilterCommand(Worksheet sheet, int tableIndex)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        this.sheet = sheet;
        this.table = tableIndex >= 0 && tableIndex < sheet.Tables.Count
            ? sheet.Tables[tableIndex]
            : null;
    }

    /// <inheritdoc/>
    public bool Execute()
    {
        if (table is null)
        {
            return true;
        }

        previousShowFilterButton = table.ShowFilterButton;
        table.ShowFilterButton = !table.ShowFilterButton;
        return true;
    }

    /// <inheritdoc/>
    public void Unexecute()
    {
        if (table is null)
        {
            return;
        }

        table.ShowFilterButton = previousShowFilterButton;
    }
}
