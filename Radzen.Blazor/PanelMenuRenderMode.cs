namespace Radzen;

/// <summary>
/// Specifies the ways a <see cref="Radzen.Blazor.RadzenPanelMenu" /> component renders the child items of its expandable items.
/// </summary>
public enum PanelMenuRenderMode
{
    /// <summary>
    /// All child items are rendered up front and collapsed ones are hidden. This preserves the expand/collapse animation but keeps the entire menu in the DOM.
    /// </summary>
    Client,

    /// <summary>
    /// Child items are rendered by Blazor only while their parent item is expanded. Collapsed branches are not rendered, which keeps the DOM small for large menus.
    /// </summary>
    Server
}
