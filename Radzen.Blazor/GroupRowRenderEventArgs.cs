using System.Collections.Generic;

namespace Radzen;

/// <summary>
/// Supplies information about a <see cref="Radzen.Blazor.RadzenDataGrid{TItem}" /> event that is being raised.
/// </summary>
public class GroupRowRenderEventArgs
{
    private IDictionary<string, object>? attributes;

    /// <summary>
    /// Gets or sets the group row HTML attributes. They will apply to the table row (tr) element which RadzenDataGrid renders for every group row.
    /// </summary>
    /// <remarks>The backing dictionary is allocated on first access, so a group row with no GroupRowRender handler pays nothing.</remarks>
    public IDictionary<string, object> Attributes => attributes ??= new Dictionary<string, object>();

    /// <summary>
    /// Whether any attributes were added, without forcing the backing dictionary to be allocated.
    /// </summary>
    internal bool HasAttributes => attributes is { Count: > 0 };

    /// <summary>
    /// Gets the data item which the current row represents.
    /// </summary>
    public Group? Group { get; internal set; }

    /// <summary>
    /// Gets or sets a value indicating whether this group row is expandable.
    /// </summary>
    /// <value><c>true</c> if expandable; otherwise, <c>false</c>.</value>
    public bool Expandable { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this group row is expanded.
    /// </summary>
    /// <value><c>true</c> if expanded; otherwise, <c>false</c>.</value>
    public bool? Expanded { get; set; }

    /// <summary>
    /// Gets a value indicating whether this is the first time the RadzenGrid has rendered.
    /// </summary>
    /// <value><c>true</c> if this is the first time; otherwise, <c>false</c>.</value>
    public bool FirstRender { get; internal set; }
}

