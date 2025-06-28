using System;

namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Represents a command to toggle filter button on a data table.
/// </summary>
public class DataTableFilterCommand : ICommand
{
    private readonly Sheet sheet;
    private readonly int dataTableIndex;
    private readonly bool previousShowFilterButton;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataTableFilterCommand"/> class.
    /// </summary>
    /// <param name="sheet">The sheet containing the data table.</param>
    /// <param name="dataTableIndex">The index of the data table to toggle filter button on.</param>
    public DataTableFilterCommand(Sheet sheet, int dataTableIndex)
    {
        this.sheet = sheet;
        this.dataTableIndex = dataTableIndex;
        this.previousShowFilterButton = dataTableIndex >= 0 && dataTableIndex < sheet.DataTables.Count 
            ? sheet.DataTables[dataTableIndex].ShowFilterButton 
            : false;
    }

    /// <inheritdoc/>
    public bool Execute()
    {
        if (dataTableIndex >= 0 && dataTableIndex < sheet.DataTables.Count)
        {
            var dataTable = sheet.DataTables[dataTableIndex];
            dataTable.ShowFilterButton = !dataTable.ShowFilterButton;
        }

        return true;
    }

    /// <inheritdoc/>
    public void Unexecute()
    {
        if (dataTableIndex >= 0 && dataTableIndex < sheet.DataTables.Count)
        {
            var dataTable = sheet.DataTables[dataTableIndex];
            dataTable.ShowFilterButton = previousShowFilterButton;
        }
    }
} 