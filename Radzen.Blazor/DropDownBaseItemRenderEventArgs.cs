using System.Collections.Generic;

namespace Radzen;

/// <summary>
/// Supplies information about RadzenDropDown ItemRender event.
/// </summary>
public class DropDownBaseItemRenderEventArgs<TValue>
{
    /// <summary>
    /// Gets the data item.
    /// </summary>
    public object Item { get; internal set; }

    /// <summary>
    /// Gets or sets a value indicating whether this item is visible.
    /// </summary>
    /// <value><c>true</c> if visible; otherwise, <c>false</c>.</value>
    public bool Visible { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether this item is disabled.
    /// </summary>
    /// <value><c>true</c> if disabled; otherwise, <c>false</c>.</value>
    public bool Disabled { get; set; }

    /// <summary>
    /// Gets or sets the row HTML attributes.
    /// </summary>
    public IDictionary<string, object> Attributes { get; private set; } = new Dictionary<string, object>();
}

