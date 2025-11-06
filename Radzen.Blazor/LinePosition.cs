namespace Radzen;

/// <summary>
/// Specifies the ways a <see cref="Radzen.Blazor.RadzenTimeline" /> component renders line and content items.
/// </summary>
public enum LinePosition
{
    /// <summary>
    /// The RadzenTimeline line is displayed at the center of the component.
    /// </summary>
    Center,

    /// <summary>
    /// The RadzenTimeline line is displayed at the center of the component with alternating content position.
    /// </summary>
    Alternate,

    /// <summary>
    /// The RadzenTimeline line is displayed at the start of the component.
    /// </summary>
    Start,

    /// <summary>
    /// The RadzenTimeline line is displayed at the end of the component.
    /// </summary>
    End,

    /// <summary>
    /// The RadzenTimeline line is displayed at the left side of the component.
    /// </summary>
    Left,

    /// <summary>
    /// The RadzenTimeline line is displayed at the right side of the component.
    /// </summary>
    Right,

    /// <summary>
    /// The RadzenTimeline line is displayed at the top of the component.
    /// </summary>
    Top,

    /// <summary>
    /// The RadzenTimeline line is displayed at the bottom of the component.
    /// </summary>
    Bottom
}

