using Radzen.Blazor;

namespace Radzen;

/// <summary>
/// Supplies information about a <see cref="RadzenDataGrid{TItem}.CellContextMenu" /> event that is being raised.
/// </summary>
/// <typeparam name="T">The data item type.</typeparam>
public class DataGridCellMouseEventArgs<T> : Microsoft.AspNetCore.Components.Web.MouseEventArgs
{
    /// <summary>
    /// Gets the data item which the clicked DataGrid row represents.
    /// </summary>
    public T Data { get; internal set; }

    /// <summary>
    /// Gets the RadzenDataGridColumn which this cells represents.
    /// </summary>
    public RadzenDataGridColumn<T> Column { get; internal set; }
}

