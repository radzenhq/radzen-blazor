namespace Radzen;

/// <summary>
/// Specifies the display style of a <see cref="Radzen.Blazor.RadzenButton" />. Affects the visual styling of RadzenButton (background and text color).
/// </summary>
public enum ButtonStyle
{
    /// <summary>
    /// A primary button. Clicking it performs the primary action in a form or dialog (e.g. "save").
    /// </summary>
    Primary,

    /// <summary>
    /// A secondary button. Clicking it performs a secondary action in a form or dialog (e.g. close a dialog or cancel a form).
    /// </summary>
    Secondary,

    /// <summary>
    /// A button with lighter styling.
    /// </summary>
    Light,

    /// <summary>
    /// The base UI styling.
    /// </summary>
    Base,

    /// <summary>
    /// A button with dark styling.
    /// </summary>
    Dark,

    /// <summary>
    /// A button with success styling.
    /// </summary>
    Success,

    /// <summary>
    /// A button which represents a dangerous action e.g. "delete".
    /// </summary>
    Danger,

    /// <summary>
    /// A button with warning styling.
    /// </summary>
    Warning,

    /// <summary>
    /// A button with informative styling.
    /// </summary>
    Info
}

