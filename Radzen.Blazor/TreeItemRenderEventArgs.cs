using System.Collections;
using System.Collections.Generic;

namespace Radzen;

/// <summary>
/// Supplies information about a <see cref="Radzen.Blazor.RadzenTree" /> item render event that is being raised.
/// </summary>
public class TreeItemRenderEventArgs
{
    /// <summary>
    /// Gets or sets the item HTML attributes.
    /// </summary>
    public IDictionary<string, object> Attributes { get; private set; } = new Dictionary<string, object>();

    private bool checkedSet;

    internal bool CheckedSet()
    {
        return checkedSet;
    }

    private bool? checkedValue;

    /// <summary>
    /// Gets or sets a value indicating whether this item is checked.
    /// </summary>
    /// <value><c>true</c> if expanded; otherwise, <c>false</c>.</value>
    public bool? Checked
    {
        get
        {
            return checkedValue;
        }
        set
        {
            checkedSet = true;
            checkedValue = value;
        }
    }

    /// <summary>
    /// Gets tree item.
    /// </summary>
    public object Value { get; internal set; }

    /// <summary>
    /// Gets child items.
    /// </summary>
    public IEnumerable Data { get; internal set; }
}

