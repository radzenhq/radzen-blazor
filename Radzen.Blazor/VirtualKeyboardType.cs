namespace Radzen;

/// <summary>
/// Specifies the set of keys displayed by <see cref="Radzen.Blazor.RadzenVirtualKeyboard" />.
/// </summary>
public enum VirtualKeyboardType
{
    /// <summary>
    /// Displays an alphanumeric keyboard with a digit row.
    /// </summary>
    Alphanumeric,

    /// <summary>
    /// Displays a numeric keypad with a culture-aware decimal separator.
    /// </summary>
    Numpad,

    /// <summary>
    /// Displays an alphanumeric keyboard together with a numeric keypad.
    /// </summary>
    All
}
