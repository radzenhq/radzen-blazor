using System.Globalization;

namespace Radzen.Blazor;

/// <summary>
/// Provides extension methods for numeric types to convert them to pixel values.
/// </summary>
public static class NumberExtensions
{
    /// <summary>
    /// Converts a double value to a string representation in pixels (px).
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string ToPx(this double value)
    {
        return $"{value.ToString(CultureInfo.InvariantCulture)}px";
    }
}