namespace Radzen;

/// <summary>
/// Specifies the type of a <see cref="Radzen.Blazor.RadzenButton" />. Renders as the <c>type</c> HTML attribute.
/// </summary>
public enum ButtonType
{
    /// <summary>
    /// Generic button which does not submit its parent form.
    /// </summary>
    Button,

    /// <summary>
    /// Clicking a submit button submits its parent form.
    /// </summary>
    Submit,

    /// <summary>
    /// Clicking a reset button clears the value of all inputs in its parent form.
    /// </summary>
    Reset
}

