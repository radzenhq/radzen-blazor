using Microsoft.AspNetCore.Components;
using Radzen.Blazor;

namespace Radzen;

/// <summary>
/// Supplies information about a <see cref="RadzenTree.Expand" /> event that is being raised.
/// </summary>
public class TreeExpandEventArgs
{
    /// <summary>
    /// Gets the <see cref="Radzen.Blazor.RadzenTreeItem.Value" /> the expanded RadzenTreeItem.
    /// </summary>
    public object Value { get; set; }

    /// <summary>
    /// Gets the <see cref="Radzen.Blazor.RadzenTreeItem.Text" /> the expanded RadzenTreeItem.
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// Gets or sets the children of the expanded RadzenTreeItem.
    /// </summary>
    public TreeItemSettings Children { get; set; }
}

