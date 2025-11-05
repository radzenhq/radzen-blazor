namespace Radzen;

/// <summary>
/// Specifies the display style or severity of a <see cref="Radzen.Blazor.RadzenAlert" />. Affects the visual styling of RadzenAlert (background and text color).
/// </summary>
public enum AlertStyle
{
    /// <summary>
    /// Primary styling. Similar to primary buttons.
    /// </summary>
    Primary,

    /// <summary>
    /// Secondary styling. Similar to secondary buttons.
    /// </summary>
    Secondary,

    /// <summary>
    /// Light styling. Similar to light buttons.
    /// </summary>
    Light,

    /// <summary>
    /// Base styling. Similar to base buttons.
    /// </summary>
    Base,

    /// <summary>
    /// Dark styling. Similar to dark buttons.
    /// </summary>
    Dark,

    /// <summary>
    /// Success styling.
    /// </summary>
    Success,

    /// <summary>
    /// Danger styling.
    /// </summary>
    Danger,

    /// <summary>
    /// Warning styling.
    /// </summary>
    Warning,

    /// <summary>
    /// Informative styling.
    /// </summary>
    Info
}

