using System;

namespace Radzen;

/// <summary>
/// Specifies the position at which a Radzen Blazor component renders its built-in <see cref="Radzen.Blazor.RadzenPager" />.
/// </summary>
[Flags]
public enum PagerPosition
{
    /// <summary>
    /// RadzenPager is displayed at the top of the component.
    /// </summary>
    Top = 1,

    /// <summary>
    /// RadzenPager is displayed at the bottom of the component.
    /// </summary>
    Bottom = 2,

    /// <summary>
    /// RadzenPager is displayed at the top and at the bottom of the component.
    /// </summary>
    TopAndBottom = Top | Bottom
}

