using Radzen.Blazor;

namespace Radzen;

/// <summary>
/// Supplies information about RadzenListBox ItemRender event.
/// </summary>
public class ListBoxItemRenderEventArgs<TValue> : DropDownBaseItemRenderEventArgs<TValue>
{
    /// <summary>
    /// Gets the ListBox.
    /// </summary>
    public RadzenListBox<TValue> ListBox { get; internal set; }
}

