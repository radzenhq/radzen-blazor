using System.Collections.Generic;

namespace Radzen;

/// <summary>
/// Supplies information about a <see cref="Radzen.Blazor.RadzenDataGrid{TItem}.LoadChildData" /> event that is being raised.
/// </summary>
/// <typeparam name="T">The data item type.</typeparam>
public class DataGridLoadChildDataEventArgs<T>
{
    /// <summary>
    /// Gets or sets the data.
    /// </summary>
    /// <value>The data.</value>
    public IEnumerable<T> Data { get; set; }

    /// <summary>
    /// Gets the item.
    /// </summary>
    /// <value>The item.</value>
    public T Item { get; internal set; }
}

