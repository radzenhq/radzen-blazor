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
    public EventCallback<SheetFilter?> Apply { get; set; }

    /// <summary>
    /// Invoked when the user clicks the sort ascending option in the cell menu.
    /// </summary>
    [Parameter]
    public EventCallback SortAscending { get; set; }

    /// <summary>
    /// Invoked when the user clicks the sort descending option in the cell menu.
    /// </summary>
    [Parameter]
    public EventCallback SortDescending { get; set; }

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

        // If there are existing filter values for this column, use them
        if (visitor.FoundValues.Any())
        {
            foreach (var value in visitor.FoundValues)
            {
                selectedFilterValues.Add(value);
            }
        }
        else
        {
            // No existing filters for this column, so select all available values by default
            var availableValues = LoadAvailableValues();
            foreach (var value in availableValues)
            {
                selectedFilterValues.Add(value.Value);
            }
            
            // Also select null if blank option should be shown
            if (ShouldShowBlankOption())
            {
                selectedFilterValues.Add(null);
            }
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

    private async Task OnSortAscendingAsync()
    {
        await SortAscending.InvokeAsync();
    }

    private async Task OnSortDescendingAsync()
    {
        await SortDescending.InvokeAsync();
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
        
        StateHasChanged();
    }

    private bool IsValueSelected(object value)
    {
        return selectedFilterValues.Contains(value);
    }

    private bool? IsSelectAllChecked()
    {
        var availableValues = LoadAvailableValues();
        var showBlank = ShouldShowBlankOption();
        
        if (!availableValues.Any() && !showBlank)
            return false;

        var totalItems = availableValues.Count + (showBlank ? 1 : 0);
        var selectedCount = availableValues.Count(v => selectedFilterValues.Contains(v.Value));
        
        // Add 1 to selected count if blank is selected
        if (showBlank && selectedFilterValues.Contains(null))
        {
            selectedCount++;
        }
        
        if (selectedCount == 0)
            return false;
        else if (selectedCount == totalItems)
            return true;
        else
            return null; // Indeterminate state
    }

    private void OnSelectAllChanged(bool? isChecked)
    {
        var availableValues = LoadAvailableValues();
        var showBlank = ShouldShowBlankOption();
        
        // For tristate checkbox behavior:
        // - When indeterminate (null) and clicked -> becomes checked (true) -> select all
        // - When checked (true) and clicked -> becomes unchecked (false) -> deselect all
        // - When unchecked (false) and clicked -> becomes checked (true) -> select all
        if (isChecked != false)
        {
            // Select all values
            foreach (var value in availableValues)
            {
                selectedFilterValues.Add(value.Value);
            }
            
            // Also select blank if it should be shown
            if (showBlank)
            {
                selectedFilterValues.Add(null);
            }
        }
        else if (isChecked == false)
        {
            // Deselect all values
            foreach (var value in availableValues)
            {
                selectedFilterValues.Remove(value.Value);
            }
            
            // Also deselect blank
            selectedFilterValues.Remove(null);
        }
        // Note: isChecked == null (indeterminate) should not happen in the Change event
        // as clicking indeterminate should transition to true
        
        StateHasChanged();
    }

    private bool ShouldShowBlankOption()
    {
        var dataTable = GetCurrentDataTable();
        var autoFilter = GetCurrentAutoFilter();

        // Determine the range to use for checking blank values
        RangeRef rangeToUse = RangeRef.Invalid;
        
        if (dataTable != null)
        {
            // Use data table range if the cell is part of a data table
            rangeToUse = dataTable.Range;
        }
        else if (autoFilter != null)
        {
            // Use auto filter range if the cell is part of an auto filter
            rangeToUse = autoFilter.Range;
        }

        if (rangeToUse == RangeRef.Invalid) return false;

        // Check if any cell in the column has null or empty value
        for (int row = rangeToUse.Start.Row; row <= rangeToUse.End.Row; row++)
        {
            var cell = Sheet.Cells[row, Column];
            var value = cell.Value;
            var text = cell.GetValue();
            
            if (value == null || string.IsNullOrEmpty(text))
            {
                return true;
            }
        }
        
        return false;
    }

    private bool IsBlankSelected()
    {
        return selectedFilterValues.Contains(null);
    }

    private void OnBlankSelectionChanged(bool isChecked)
    {
        if (isChecked)
        {
            selectedFilterValues.Add(null);
        }
        else
        {
            selectedFilterValues.Remove(null);
        }
        
        StateHasChanged();
    }

    private async Task OnApplyFilterAsync()
    {
        SheetFilter? filter = null;
        
        if (selectedFilterValues.Any())
        {
            var dataTable = GetCurrentDataTable();
            var autoFilter = GetCurrentAutoFilter();

            // Determine the range to use for the filter
            RangeRef rangeToUse = RangeRef.Invalid;
            
            if (dataTable != null)
            {
                // Use data table range if the cell is part of a data table
                rangeToUse = dataTable.Range;
            }
            else if (autoFilter != null)
            {
                // Use auto filter range if the cell is part of an auto filter
                rangeToUse = autoFilter.Range;
            }

            if (rangeToUse != RangeRef.Invalid)
            {
                // Create a new range for the current column using the determined range
                var columnRange = new RangeRef(
                    new CellRef(rangeToUse.Start.Row, Column),
                    new CellRef(rangeToUse.End.Row, Column)
                );

                filter = new SheetFilter(
                    new InListCriterion
                    {
                        Column = Column,
                        Values = [.. selectedFilterValues]
                    },
                    columnRange
                );
            }
        }

        await Apply.InvokeAsync(filter);
    }

    private bool CanApplyFilter()
    {
        return selectedFilterValues.Any();
    }

    private List<(string Text, object? Value)> LoadAvailableValues()
    {
        var availableValues = new List<(string Text, object? Value)>();
        var dataTable = GetCurrentDataTable();
        var autoFilter = GetCurrentAutoFilter();

        // Determine the range to use for loading values
        RangeRef rangeToUse = RangeRef.Invalid;
        
        if (dataTable != null)
        {
            // Use data table range if the cell is part of a data table
            rangeToUse = dataTable.Range;
        }
        else if (autoFilter != null)
        {
            // Use auto filter range if the cell is part of an auto filter
            rangeToUse = autoFilter.Range;
        }

        if (rangeToUse != RangeRef.Invalid)
        {
            var uniqueValues = new List<(string Text, object? Value)>();

            // Get all values from the column in the determined range
            for (int row = rangeToUse.Start.Row; row <= rangeToUse.End.Row; row++)
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

    private AutoFilter? GetCurrentAutoFilter()
    {
        // Check if the sheet has an auto filter and if the current cell is within its range
        if (Sheet.AutoFilter != null && Sheet.AutoFilter.Range.Contains(Row, Column))
        {
            return Sheet.AutoFilter;
        }
        return null;
    }
}