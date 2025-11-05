using Radzen.Blazor;

namespace Radzen;

/// <summary>
/// Supplies information about a <see cref="RadzenDataGrid{TItem}.ColumnReordering" /> event that is being raised.
/// </summary>
/// <typeparam name="T">The data item type.</typeparam>
public class DataGridColumnReorderingEventArgs<T>
{
    /// <summary>
    /// Gets the reordered RadzenDataGridColumn.
    /// </summary>
    public RadzenDataGridColumn<T> Column { get; internal set; }

    /// <summary>
    /// Gets the reordered to RadzenDataGridColumn.
    /// </summary>
    public RadzenDataGridColumn<T> ToColumn { get; internal set; }

    /// <summary>
    /// Gets or sets a value which will cancel the event.
    /// </summary>
    /// <value><c>true</c> to cancel the event; otherwise, <c>false</c>.</value>
    public bool Cancel { get; set; }
}

