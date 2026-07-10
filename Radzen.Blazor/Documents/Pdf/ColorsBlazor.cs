namespace Radzen.Blazor.Pdf;

#nullable enable

// Consumers whose own namespace is nested under Radzen (e.g. Radzen.Blazor.Pdf.*) cannot reach
// Radzen.Documents.Pdf.Colors through a using directive: the enclosing Radzen.Colors shadows it.
// This mirror lives in an enclosing namespace of those consumers so the unqualified name resolves
// to the PDF palette. It forwards to the canonical Radzen.Documents.Pdf.Colors.

/// <summary>
/// Predefined CSS named colors for consumers under the <c>Radzen.Blazor</c> namespace.
/// Forwards to <see cref="Radzen.Documents.Pdf.Colors"/>.
/// </summary>
public static class Colors
{
    /// <summary>Black (#000000).</summary>
    public static Documents.Pdf.Color Black => Documents.Pdf.Colors.Black;

    /// <summary>White (#FFFFFF).</summary>
    public static Documents.Pdf.Color White => Documents.Pdf.Colors.White;

    /// <summary>Red (#FF0000).</summary>
    public static Documents.Pdf.Color Red => Documents.Pdf.Colors.Red;

    /// <summary>Green (#008000).</summary>
    public static Documents.Pdf.Color Green => Documents.Pdf.Colors.Green;

    /// <summary>Blue (#0000FF).</summary>
    public static Documents.Pdf.Color Blue => Documents.Pdf.Colors.Blue;

    /// <summary>Yellow (#FFFF00).</summary>
    public static Documents.Pdf.Color Yellow => Documents.Pdf.Colors.Yellow;

    /// <summary>Orange (#FFA500).</summary>
    public static Documents.Pdf.Color Orange => Documents.Pdf.Colors.Orange;

    /// <summary>Gray (#808080).</summary>
    public static Documents.Pdf.Color Gray => Documents.Pdf.Colors.Gray;

    /// <summary>Light gray (#D3D3D3).</summary>
    public static Documents.Pdf.Color LightGray => Documents.Pdf.Colors.LightGray;

    /// <summary>Dark gray (#A9A9A9).</summary>
    public static Documents.Pdf.Color DarkGray => Documents.Pdf.Colors.DarkGray;

    /// <summary>Dark blue (#00008B).</summary>
    public static Documents.Pdf.Color DarkBlue => Documents.Pdf.Colors.DarkBlue;

    /// <summary>Transparent (#00000000).</summary>
    public static Documents.Pdf.Color Transparent => Documents.Pdf.Colors.Transparent;
}
