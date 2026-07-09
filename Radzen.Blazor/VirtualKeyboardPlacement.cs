namespace Radzen;

/// <summary>
/// Specifies where <see cref="Radzen.Blazor.RadzenVirtualKeyboard" /> displays its keys when an input gets focus.
/// </summary>
public enum VirtualKeyboardPlacement
{
    /// <summary>
    /// The keyboard opens as a bar fixed to the bottom of the viewport.
    /// </summary>
    Bottom,

    /// <summary>
    /// The keyboard opens as a bar fixed to the top of the viewport.
    /// </summary>
    Top,

    /// <summary>
    /// The keyboard opens as a popup next to the focused input.
    /// </summary>
    Auto,

    /// <summary>
    /// The keyboard is always visible and rendered in place.
    /// </summary>
    Inline
}
