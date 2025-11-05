using Radzen.Blazor;

namespace Radzen;

/// <summary>
/// Supplies information about a <see cref="RadzenDataGrid{TItem}.ColumnReordered" /> event that is being raised.
/// </summary>
/// <typeparam name="T">The data item type.</typeparam>
public class DataGridColumnReorderedEventArgs<T>
{
    /// <summary>
    /// Gets the reordered RadzenDataGridColumn.
    /// </summary>
    public RadzenDataGridColumn<T> Column { get; internal set; }

    /// <summary>
    /// Gets the old index of the column.
    /// </summary>
    public int OldIndex { get; internal set; }

    /// <summary>
    /// Gets the new index of the column.
    /// </summary>
    public int NewIndex { get; internal set; }
}

