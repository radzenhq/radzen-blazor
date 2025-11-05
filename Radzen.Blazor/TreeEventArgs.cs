namespace Radzen;

/// <summary>
/// Supplies information about a <see cref="Radzen.Blazor.RadzenTree.Change" /> event that is being raised.
/// </summary>
public class TreeEventArgs
{
    /// <summary>
    /// Gets the <see cref="Radzen.Blazor.RadzenTreeItem.Text" /> the selected RadzenTreeItem.
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// Gets the <see cref="Radzen.Blazor.RadzenTreeItem.Value" /> the selected RadzenTreeItem.
    /// </summary>
    public object Value { get; set; }
}

