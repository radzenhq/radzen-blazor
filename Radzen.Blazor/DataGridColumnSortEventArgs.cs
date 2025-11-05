using Radzen.Blazor;

namespace Radzen;

/// <summary>
/// Supplies information about a <see cref="RadzenDataGrid{TItem}.Sort" /> event that is being raised.
/// </summary>
/// <typeparam name="T">The data item type.</typeparam>
public class DataGridColumnSortEventArgs<T>
{
    /// <summary>
    /// Gets the sorted RadzenDataGridColumn.
    /// </summary>
    public RadzenDataGridColumn<T> Column { get; internal set; }

    /// <summary>
    /// Gets the new sort order of the sorted column.
    /// </summary>
    public SortOrder? SortOrder { get; internal set; }
}

