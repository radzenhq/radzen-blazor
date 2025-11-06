namespace Radzen;

/// <summary>
/// Specifies the ways a <see cref="Radzen.Blazor.RadzenTabs" /> component renders its items.
/// </summary>
public enum TabRenderMode
{
    /// <summary>
    /// The RadzenTabs component switches its items server side. Only the selected item is rendered.
    /// </summary>
    Server,

    /// <summary>
    /// The RadzenTabs components switches its items client-side. All items are rendered and the unselected ones are hidden with CSS.
    /// </summary>
    Client
}

