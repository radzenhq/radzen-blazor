namespace Radzen;

/// <summary>
/// Supplies information about a <see cref="Radzen.Blazor.RadzenTree.ItemContextMenu" /> event that is being raised.
/// </summary>
public class TreeItemContextMenuEventArgs : Microsoft.AspNetCore.Components.Web.MouseEventArgs
{
    /// <summary>
    /// Gets the tree item text.
    /// </summary>
    public string Text { get; internal set; }

    /// <summary>
    /// Gets the tree item value.
    /// </summary>
    public object Value { get; internal set; }
}

