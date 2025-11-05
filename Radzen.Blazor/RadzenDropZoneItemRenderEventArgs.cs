using System.Collections.Generic;
using Radzen.Blazor;

namespace Radzen;

/// <summary>
/// Supplies information about RadzenDropZoneContainer ItemRender event.
/// </summary>
public class RadzenDropZoneItemRenderEventArgs<TItem>
{
    /// <summary>
    /// Gets the drop zone.
    /// </summary>
    public RadzenDropZone<TItem> Zone { get; internal set; }

    /// <summary>
    /// Gets the dragged item.
    /// </summary>
    public TItem Item { get; internal set; }

    /// <summary>
    /// Gets or sets a value indicating whether this item is visible.
    /// </summary>
    /// <value><c>true</c> if visible; otherwise, <c>false</c>.</value>
    public bool Visible { get; set; } = true;

    /// <summary>
    /// Gets or sets the row HTML attributes.
    /// </summary>
    public IDictionary<string, object> Attributes { get; private set; } = new Dictionary<string, object>();
}

