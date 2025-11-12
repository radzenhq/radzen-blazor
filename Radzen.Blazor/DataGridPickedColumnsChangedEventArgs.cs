using System.Collections.Generic;
using Radzen.Blazor;

namespace Radzen;

/// <summary>
/// Supplies information about a <see cref="RadzenDataGrid{TItem}.PickedColumnsChanged" /> event that is being raised.
/// </summary>
/// <typeparam name="T">The data item type.</typeparam>
public class DataGridPickedColumnsChangedEventArgs<T> where T : notnull
{
    /// <summary>
    /// Gets the picked columns.
    /// </summary>
    public IEnumerable<RadzenDataGridColumn<T>>? Columns { get; internal set; }
}

