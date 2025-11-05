using System.Collections.Generic;

namespace Radzen;

/// <summary>
/// Supplies information about a <see cref="Radzen.Blazor.RadzenDataGrid{TItem}" /> event that is being raised.
/// </summary>
public class GroupRowRenderEventArgs
{
    /// <summary>
    /// Gets or sets the group row HTML attributes. They will apply to the table row (tr) element which RadzenDataGrid renders for every group row.
    /// </summary>
    public IDictionary<string, object> Attributes { get; private set; } = new Dictionary<string, object>();

    /// <summary>
    /// Gets the data item which the current row represents.
    /// </summary>
    public Group Group { get; internal set; }

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

