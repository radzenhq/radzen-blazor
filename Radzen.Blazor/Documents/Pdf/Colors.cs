namespace Radzen.Documents.Pdf;

#nullable enable

/// <summary>
/// Predefined CSS named colors.
/// </summary>
public static class Colors
{
    /// <summary>Black (#000000).</summary>
    public static Color Black => Color.FromRgb(0, 0, 0);

    /// <summary>White (#FFFFFF).</summary>
    public static Color White => Color.FromRgb(255, 255, 255);

    /// <summary>Red (#FF0000).</summary>
    public static Color Red => Color.FromRgb(255, 0, 0);

    /// <summary>Green (#008000).</summary>
    public static Color Green => Color.FromRgb(0, 128, 0);

    /// <summary>Blue (#0000FF).</summary>
    public static Color Blue => Color.FromRgb(0, 0, 255);

    /// <summary>Yellow (#FFFF00).</summary>
    public static Color Yellow => Color.FromRgb(255, 255, 0);

    /// <summary>Orange (#FFA500).</summary>
    public static Color Orange => Color.FromRgb(255, 165, 0);

    /// <summary>Gray (#808080).</summary>
    public static Color Gray => Color.FromRgb(128, 128, 128);

    /// <summary>Light gray (#D3D3D3).</summary>
    public static Color LightGray => Color.FromRgb(211, 211, 211);

    /// <summary>Dark gray (#A9A9A9).</summary>
    public static Color DarkGray => Color.FromRgb(169, 169, 169);

    /// <summary>Dark blue (#00008B).</summary>
    public static Color DarkBlue => Color.FromRgb(0, 0, 139);

    /// <summary>Transparent (#00000000).</summary>
    public static Color Transparent => Color.FromArgb(0, 0, 0, 0);
}
