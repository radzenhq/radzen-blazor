namespace Radzen;

/// <summary>
/// Specifies the behavior of <see cref="Radzen.Blazor.RadzenProgressBar" /> or <see cref="Radzen.Blazor.RadzenProgressBarCircular" />.
/// </summary>
public enum ProgressBarMode
{
    /// <summary>
    /// RadzenProgressBar displays its value as a percentage range (0 to 100).
    /// </summary>
    Determinate,

    /// <summary>
    /// RadzenProgressBar displays continuous animation.
    /// </summary>
    Indeterminate
}

