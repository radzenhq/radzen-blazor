namespace Radzen;

/// <summary>
/// Specifies the transition animation used when switching between steps in a <see cref="Radzen.Blazor.RadzenSteps" /> component.
/// </summary>
public enum StepsTransition
{
    /// <summary>
    /// No transition animation. Step content changes instantly (default behavior).
    /// </summary>
    None,

    /// <summary>
    /// Fade-in transition. The new step content fades in.
    /// </summary>
    Fade,

    /// <summary>
    /// Slide transition. The new step content slides in horizontally. Automatically respects RTL direction.
    /// </summary>
    Slide
}
