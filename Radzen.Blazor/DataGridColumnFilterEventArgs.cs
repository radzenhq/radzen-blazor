using Radzen.Blazor;

namespace Radzen;

/// <summary>
/// Supplies information about a <see cref="RadzenDataGrid{TItem}.Filter" /> event that is being raised.
/// </summary>
/// <typeparam name="T">The data item type.</typeparam>
public class DataGridColumnFilterEventArgs<T>
{
    /// <summary>
    /// Gets the filtered RadzenDataGridColumn.
    /// </summary>
    public RadzenDataGridColumn<T> Column { get; internal set; }

    /// <summary>
    /// Gets the new filter value of the filtered column.
    /// </summary>
    public object FilterValue { get; internal set; }

    /// <summary>
    /// Gets the new second filter value of the filtered column.
    /// </summary>
    public object SecondFilterValue { get; internal set; }

    /// <summary>
    /// Gets the new filter operator of the filtered column.
    /// </summary>
    public FilterOperator FilterOperator { get; internal set; }

    /// <summary>
    /// Gets the new second filter operator of the filtered column.
    /// </summary>
    public FilterOperator SecondFilterOperator { get; internal set; }

    /// <summary>
    /// Gets the new logical filter operator of the filtered column.
    /// </summary>
    public LogicalFilterOperator LogicalFilterOperator { get; internal set; }
}

