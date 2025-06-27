using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Represents a cell menu in a spreadsheet.
/// </summary>
public partial class CellMenu : ComponentBase
{
    /// <summary>
    /// Represents the sheet containing the cell menu.
    /// </summary>
    [Parameter, EditorRequired]
    public Sheet Sheet { get; set; } = default!;

    /// <summary>
    /// Represents the row index of the cell menu.
    /// </summary>
    [Parameter, EditorRequired]
    public int Row { get; set; }

    /// <summary>
    /// Represents the column index of the cell menu.
    /// </summary>
    [Parameter, EditorRequired]
    public int Column { get; set; }

    /// <summary>
    /// Invoked the user clicks the cancel button in the cell menu.
    /// </summary>
    [Parameter]
    public EventCallback Cancel { get; set; }

    /// <summary>
    /// Invoked the user clicks the apply button in the cell menu.
    /// </summary>
    [Parameter]
    public EventCallback Apply { get; set; }

    private readonly HashSet<object?> selectedFilterValues = [];

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();

        InitializeSelectedFilterValues();
    }

    private void InitializeSelectedFilterValues()
    {
        selectedFilterValues.Clear();

        var visitor = new InListCriterionVisitor(Column);

        foreach (var filter in Sheet.Filters)
        {
            filter.Criterion.Accept(visitor);
        }

        // Add any found values to the selected set
        foreach (var value in visitor.FoundValues)
        {
            selectedFilterValues.Add(value);
        }
    }

    private class InListCriterionVisitor(int column) : IFilterCriterionVisitor
    {
        private readonly int targetColumn = column;
        public HashSet<object?> FoundValues { get; } = [];

        public void Visit(OrCriterion criterion)
        {
            foreach (var c in criterion.Criteria)
            {
                c.Accept(this);
            }
        }

        public void Visit(AndCriterion criterion)
        {
            foreach (var c in criterion.Criteria)
            {
                c.Accept(this);
            }
        }

        public void Visit(EqualsCriterion criterion)
        {
            // Not relevant for InListCriterion extraction
        }

        public void Visit(GreaterThanCriterion criterion)
        {
            // Not relevant for InListCriterion extraction
        }

        public void Visit(InListCriterion criterion)
        {
            if (criterion.Column == targetColumn)
            {
                foreach (var value in criterion.Values)
                {
                    FoundValues.Add(value);
                }
            }
        }

        public void Visit(IsNullCriterion criterion)
        {
            // Not relevant for InListCriterion extraction
        }
    }

    private void OnSortAscendingAsync()
    {
        foreach (var dataTable in Sheet.DataTables)
        {
            if (dataTable.Range.Contains(Row, Column))
            {
                dataTable.Sort(SortOrder.Ascending, Column);
                break;
            }
        }
    }

    private void OnSortDescendingAsync()
    {
        foreach (var dataTable in Sheet.DataTables)
        {
            if (dataTable.Range.Contains(Row, Column))
            {
                dataTable.Sort(SortOrder.Descending, Column);
                break;
            }
        }
    }

    private async Task OnCancelFilterAsync()
    {
        await Cancel.InvokeAsync();
    }

    private void OnValueSelectionChanged(object value, bool isChecked)
    {
        if (isChecked)
        {
            selectedFilterValues.Add(value);
        }
        else
        {
            selectedFilterValues.Remove(value);
        }
    }

    private bool IsValueSelected(object value)
    {
        return selectedFilterValues.Contains(value);
    }

    private async Task OnApplyFilterAsync()
    {
        if (selectedFilterValues.Any())
        {
            var dataTable = GetCurrentDataTable();
            if (dataTable != null)
            {
                // Create a new range for the current column using the data table's row range
                var columnRange = new RangeRef(
                    new CellRef(dataTable.Range.Start.Row, Column),
                    new CellRef(dataTable.Range.End.Row, Column)
                );

                var filter = new SheetFilter(
                new InListCriterion
                {
                    Column = Column,
                    Values = [.. selectedFilterValues]
                },
                columnRange
                );

                Sheet.AddFilter(filter);
            }
        }

        await Apply.InvokeAsync();
    }

    private List<(string Text, object? Value)> LoadAvailableValues()
    {
        var availableValues = new List<(string Text, object? Value)>();
        var dataTable = GetCurrentDataTable();

        if (dataTable != null)
        {
            var uniqueValues = new List<(string Text, object? Value)>();

            // Get all values from the column in the data table range
            for (int row = dataTable.Range.Start.Row; row <= dataTable.Range.End.Row; row++)
            {
                // Check if this row is hidden by a filter that affects the current column
                bool shouldSkipRow = false;
                
                foreach (var filter in Sheet.Filters)
                {
                    // If the filter affects the current column, we should include the value
                    // regardless of whether the row is hidden
                    if (filter.Range.Contains(row, Column))
                    {
                        continue;
                    }
                    
                    // If the filter affects a different range and the row is hidden, skip it
                    if (Sheet.Rows.IsHidden(row))
                    {
                        shouldSkipRow = true;
                        break;
                    }
                }

                if (shouldSkipRow)
                {
                    continue;
                }

                var cell = Sheet.Cells[row, Column];
                var value = cell.Value;
                var text = cell.GetValue();

                if (!string.IsNullOrEmpty(text))
                {
                    uniqueValues.Add((text, value));
                }
            }

            availableValues = [.. uniqueValues
                .DistinctBy(x => x.Value)
                .OrderBy(x => x.Text)];
        }

        return availableValues;
    }

    private DataTable? GetCurrentDataTable()
    {
        foreach (var dataTable in Sheet.DataTables)
        {
            if (dataTable.Range.Contains(Row, Column))
            {
                return dataTable;
            }
        }
        return null;
    }
}