namespace Radzen;

/// <summary>
/// Supplies information about a <see cref="Radzen.Blazor.RadzenDataGrid{TItem}.RowClick" /> or <see cref="Radzen.Blazor.RadzenDataGrid{TItem}.RowDoubleClick" /> event that is being raised.
/// </summary>
/// <typeparam name="T">The data item type.</typeparam>
public class DataGridRowMouseEventArgs<T> : Microsoft.AspNetCore.Components.Web.MouseEventArgs
{
    /// <summary>
    /// Gets the data item which the clicked DataGrid row represents.
    /// </summary>
    public T Data { get; internal set; }
}

