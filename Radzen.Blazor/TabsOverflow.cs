namespace Radzen;

/// <summary>
/// Specifies how a <see cref="Radzen.Blazor.RadzenTabs" /> component lays out tab titles that do not fit in the available space.
/// </summary>
public enum TabsOverflow
{
    /// <summary>
    /// The tab titles shrink to their minimum size and the tab list overflows its container. This is the default.
    /// </summary>
    Default,

    /// <summary>
    /// The tab titles keep their size and wrap to additional lines.
    /// </summary>
    Wrap,

    /// <summary>
    /// The tab titles keep their size and the tab list scrolls.
    /// </summary>
    Scroll,

    /// <summary>
    /// The tab titles keep their size and the ones that do not fit are collapsed into a dropdown menu opened from a button at the end of the tab list.
    /// The selected tab always stays visible.
    /// </summary>
    Collapse
}
