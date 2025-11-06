namespace Radzen;

/// <summary>
/// Specifies the display style of a <see cref="Radzen.Blazor.RadzenProgressBar" /> and <see cref="Radzen.Blazor.RadzenProgressBarCircular" />. Affects the visual styling of RadzenProgressBar (background and text color) and RadzenProgressBarCircular (stroke and text color).
/// </summary>
public enum ProgressBarStyle
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

