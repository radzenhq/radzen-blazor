using Radzen.Blazor;

namespace Radzen;

/// <summary>
/// Supplies information about a <see cref="RadzenDataGrid{TItem}.CellRender" /> event that is being raised.
/// </summary>
/// <typeparam name="T">The data item type.</typeparam>
public class DataGridCellRenderEventArgs<T> : RowRenderEventArgs<T>
{
    /// <summary>
    /// Gets the RadzenDataGridColumn which this cells represents.
    /// </summary>
    public RadzenDataGridColumn<T> Column { get; internal set; }
}

