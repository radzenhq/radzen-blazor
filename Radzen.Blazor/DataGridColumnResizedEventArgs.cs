using Radzen.Blazor;

namespace Radzen;

/// <summary>
/// Supplies information about a <see cref="RadzenDataGrid{TItem}.ColumnResized" /> event that is being raised.
/// </summary>
/// <typeparam name="T">The data item type.</typeparam>
public class DataGridColumnResizedEventArgs<T>
{
    /// <summary>
    /// Gets the resized RadzenDataGridColumn.
    /// </summary>
    public RadzenDataGridColumn<T> Column { get; internal set; }

    /// <summary>
    /// Gets the new width of the resized column.
    /// </summary>
    public double Width { get; internal set; }
}

