using System.Collections.Generic;
using Radzen.Blazor;

namespace Radzen;

/// <summary>
/// Supplies information about a <see cref="RadzenDataGrid{TItem}.ColumnResized" /> event that is being raised.
/// </summary>
/// <typeparam name="T">The data item type.</typeparam>
public class DataGridColumnResizedEventArgs<T> where T : notnull
{
    /// <summary>
    /// Gets the resized RadzenDataGridColumn.
    /// </summary>
    public RadzenDataGridColumn<T>? Column { get; internal set; }

    /// <summary>
    /// Gets the new width of the resized column.
    /// </summary>
    public double Width { get; internal set; }

    /// <summary>
    /// Gets the new width in pixels of every column the resize pinned a width on. Resizing one
    /// column fixes the width of its neighbours as well, so handlers which persist column widths
    /// themselves need all of them. Columns left without a width of their own are absent here.
    /// </summary>
    public IReadOnlyDictionary<RadzenDataGridColumn<T>, double> Widths { get; internal set; }
        = new Dictionary<RadzenDataGridColumn<T>, double>();
}

