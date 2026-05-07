using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Represents a cell menu in a spreadsheet.
/// </summary>
public partial class CellMenu : ComponentBase
{
    /// <summary>
    /// The parent spreadsheet component.
    /// </summary>
    [CascadingParameter]
    public ISpreadsheet? Spreadsheet { get; set; }

    /// <summary>
    /// Represents the sheet containing the cell menu.
    /// </summary>
    [Parameter]
    public Worksheet Worksheet { get; set; } = default!;

    /// <summary>
    /// Represents the row index of the cell menu.
    /// </summary>
    [Parameter]
    public int Row { get; set; }

    /// <summary>
    /// Represents the column index of the cell menu.
    /// </summary>
    [Parameter]
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

    /// <summary>
    /// Invoked when the user clicks the clear filter option in the cell menu.
    /// </summary>
    [Parameter]
    public EventCallback Clear { get; set; }

    /// <summary>
    /// Invoked when the user clicks the custom filter option in the cell menu.
    /// </summary>
    [Parameter]
    public EventCallback CustomFilter { get; set; }

    private readonly HashSet<object?> selectedFilterValues = [];
    private List<(string Text, object? Value)>? cachedAvailableValues;
    private bool? cachedShowBlank;

    /// <inheritdoc />
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        var didRowChange = parameters.TryGetValue<int>(nameof(Row), out var row) && Row != row;
        var didColumnChange = parameters.TryGetValue<int>(nameof(Column), out var column) && Column != column;
        var didSheetChange = parameters.TryGetValue<Worksheet>(nameof(Worksheet), out var sheet) && Worksheet != sheet;

        await base.SetParametersAsync(parameters);

        // Check if any of the key parameters have changed
        if (didRowChange || didColumnChange || didSheetChange)
        {
            InvalidateCache();
            // Reinitialize the selected filter values
            InitializeSelectedFilterValues();
        }
    }

    private void InvalidateCache()
    {
        cachedAvailableValues = null;
        cachedShowBlank = null;
    }

    private void InitializeSelectedFilterValues()
    {
        selectedFilterValues.Clear();

        var visitor = new InListCriterionVisitor(Column);

        foreach (var filter in Worksheet.Filters)
        {
            filter.Criterion.Accept(visitor);
        }

        if (visitor.FoundValues.Count != 0)
        {
            foreach (var value in visitor.FoundValues)
            {
                if (value is string str)
                {
                    CellData.TryConvertFromString(str, out var parsedValue, out _);
                    selectedFilterValues.Add(parsedValue);
                }
                else
                {
                    selectedFilterValues.Add(value);
                }
            }
        }
        else
        {
            var availableValues = LoadAvailableValues();

            foreach (var (_, Value) in availableValues)
            {
                selectedFilterValues.Add(Value);
            }
            
            if (ShouldShowBlankOption())
            {
                selectedFilterValues.Add(null);
            }
        }
    }

    private class InListCriterionVisitor(int column) : FilterCriterionVisitorBase
    {
        private readonly int targetColumn = column;
        public HashSet<object?> FoundValues { get; } = [];

        public override void Visit(InListCriterion criterion)
        {
            if (criterion.Column == targetColumn)
            {
                foreach (var value in criterion.Values)
                {
                    FoundValues.Add(value);
                }
            }
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

    private async Task OnClearFilterAsync()
    {
        await Clear.InvokeAsync();
    }

    private async Task OnCancelFilterAsync()
    {
        await Cancel.InvokeAsync();
    }

    private void OnValueSelectionChanged(object? value, bool isChecked)
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

    private bool IsValueSelected(object? value)
    {
        return selectedFilterValues.Contains(value);
    }

    private bool? IsSelectAllChecked()
    {
        var availableValues = LoadAvailableValues();
        var showBlank = ShouldShowBlankOption();

        if (availableValues.Count == 0 && !showBlank)
        {
            return false;
        }

        var totalItems = availableValues.Count + (showBlank ? 1 : 0);
        var selectedCount = availableValues.Count(v => selectedFilterValues.Contains(v.Value));
        
        // Add 1 to selected count if blank is selected
        if (showBlank && selectedFilterValues.Contains(null))
        {
            selectedCount++;
        }

        return selectedCount switch
        {
            0 => false, // No items selected
            var count when count == totalItems => true, // All items selected
            _ => null // Indeterminate state
        };
    }

    private void OnSelectAllChanged(bool? isChecked)
    {
        var availableValues = LoadAvailableValues();
        var showBlank = ShouldShowBlankOption();
        
        if (isChecked != false)
        {
            foreach (var (_, Value) in availableValues)
            {
                selectedFilterValues.Add(Value);
            }
            
            if (showBlank)
            {
                selectedFilterValues.Add(null);
            }
        }
        else
        {

            foreach (var (_, Value) in availableValues)
            {
                selectedFilterValues.Remove(Value);
            }
            
            selectedFilterValues.Remove(null);
        }
    }

    private bool ShouldShowBlankOption()
    {
        if (cachedShowBlank.HasValue)
        {
            return cachedShowBlank.Value;
        }

        var result = ComputeShouldShowBlankOption();
        cachedShowBlank = result;
        return result;
    }

    private bool ComputeShouldShowBlankOption()
    {
        var rangeToUse = GetActiveFilterRange();

        if (rangeToUse == RangeRef.Invalid) return false;

        // Excel treats the first row as a header and excludes it from blank checking
        // Check if any cell in the column (excluding header) has null or empty value
        for (int row = rangeToUse.Start.Row + 1; row <= rangeToUse.End.Row; row++)
        {
            var cell = Worksheet.Cells[row, Column];
            var value = cell.Value;
            var text = cell.GetValue();
            
            if (value is null || string.IsNullOrEmpty(text))
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
    }

    private async Task OnApplyFilterAsync()
    {
        var availableValues = LoadAvailableValues();
        var showBlank = ShouldShowBlankOption();
        var totalItems = availableValues.Count + (showBlank ? 1 : 0);
        var selectedCount = availableValues.Count(v => selectedFilterValues.Contains(v.Value));
        
        // Add 1 to selected count if blank is selected
        if (showBlank && selectedFilterValues.Contains(null))
        {
            selectedCount++;
        }

        // If all items are selected, clear the filter instead
        if (selectedCount == totalItems && totalItems > 0)
        {
            await Clear.InvokeAsync();
            return;
        }

        SheetFilter? filter = null;
        
        if (selectedFilterValues.Count != 0)
        {
            var rangeToUse = GetActiveFilterRange();

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
        return selectedFilterValues.Count != 0;
    }

    private bool HasFilterApplied()
    {
        foreach (var filter in Worksheet.Filters)
        {
            if (filter.Range.Contains(Row, Column))
            {
                return true;
            }
        }
        return false;
    }

    private List<(string Text, object? Value)> LoadAvailableValues()
    {
        if (cachedAvailableValues is not null)
        {
            return cachedAvailableValues;
        }

        cachedAvailableValues = ComputeAvailableValues();
        return cachedAvailableValues;
    }

    private List<(string Text, object? Value)> ComputeAvailableValues()
    {
        var availableValues = new List<(string Text, object? Value)>();
        var rangeToUse = GetActiveFilterRange();

        if (rangeToUse != RangeRef.Invalid)
        {
            var uniqueValues = new List<(string Text, object? Value)>();

            // Excel treats the first row as a header and excludes it from the available values
            // Start from the second row (header row + 1)
            for (int row = rangeToUse.Start.Row + 1; row <= rangeToUse.End.Row; row++)
            {
                // Check if this row is hidden by a filter that affects the current column
                bool shouldSkipRow = false;
                
                foreach (var filter in Worksheet.Filters)
                {
                    // If the filter affects the current column, we should include the value
                    // regardless of whether the row is hidden
                    if (filter.Range.Contains(row, Column))
                    {
                        continue;
                    }
                    
                    // If the filter affects a different range and the row is hidden, skip it
                    if (Worksheet.Rows.IsHidden(row))
                    {
                        shouldSkipRow = true;
                        break;
                    }
                }

                if (shouldSkipRow)
                {
                    continue;
                }

                var cell = Worksheet.Cells[row, Column];
                var value = cell.Value;
                var text = cell.GetValueAsString();

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

    private RangeRef GetActiveFilterRange()
    {
        foreach (var table in Worksheet.Tables)
        {
            if (table.Range.Contains(Row, Column))
            {
                return table.Range;
            }
        }

        if (Worksheet.AutoFilter.Range is not null && Worksheet.AutoFilter.Range.Value.Contains(Row, Column))
        {
            return Worksheet.AutoFilter.Range.Value;
        }

        return RangeRef.Invalid;
    }

    private async Task OnCustomFilterAsync()
    {
        await CustomFilter.InvokeAsync();
    }

    /// <summary>
    /// Invoked when the user clicks "Top 10..." in the filter dropdown.
    /// </summary>
    [Parameter]
    public EventCallback Top10Filter { get; set; }

    /// <summary>
    /// Invoked when the user clicks one of the dynamic filter shortcut items.
    /// </summary>
    [Parameter]
    public EventCallback<DynamicFilterType> DynamicFilter { get; set; }

    private async Task OnTop10FilterAsync()
    {
        await Top10Filter.InvokeAsync();
    }

    private async Task OnDynamicFilterAsync(DynamicFilterType type)
    {
        await DynamicFilter.InvokeAsync(type);
    }
}