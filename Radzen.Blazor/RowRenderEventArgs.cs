using System.Collections.Generic;

namespace Radzen;

/// <summary>
/// Supplies information about a <see cref="Radzen.Blazor.RadzenDataGrid{TItem}" /> event that is being raised.
/// </summary>
/// <typeparam name="T">The data item type.</typeparam>
public class RowRenderEventArgs<T>
{
    private IDictionary<string, object>? attributes;

    /// <summary>
    /// Gets or sets the row HTML attributes. They will apply to the table row (tr) element which RadzenDataGrid renders for every row.
    /// </summary>
    /// <remarks>The backing dictionary is allocated on first access, so a row with no RowRender handler pays nothing.</remarks>
    public IDictionary<string, object> Attributes => attributes ??= new Dictionary<string, object>();

    /// <summary>
    /// Whether any attributes were added, without forcing the backing dictionary to be allocated.
    /// </summary>
    internal bool HasAttributes => attributes is { Count: > 0 };

    /// <summary>
    /// Gets the data item which the current row represents.
    /// </summary>
    public T? Data { get; internal set; }

    /// <summary>
    /// Gets or sets a value indicating whether this row is expandable.
    /// </summary>
    /// <value><c>true</c> if expandable; otherwise, <c>false</c>.</value>
    public bool Expandable { get; set; }

    /// <summary>
    /// Gets or sets a value indicating row index.
    /// </summary>
    public int Index { get; set; }
}

