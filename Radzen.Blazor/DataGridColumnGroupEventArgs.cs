using Radzen.Blazor;

namespace Radzen;

/// <summary>
/// Supplies information about a <see cref="RadzenDataGrid{TItem}.Group" /> event that is being raised.
/// </summary>
/// <typeparam name="T">The data item type.</typeparam>
public class DataGridColumnGroupEventArgs<T>
{
    /// <summary>
    /// Gets the grouped RadzenDataGridColumn.
    /// </summary>
    public RadzenDataGridColumn<T> Column { get; internal set; }

    /// <summary>
    /// Gets the group descriptor.
    /// </summary>
    public GroupDescriptor GroupDescriptor { get; internal set; }
}

