using System;
using Radzen.Documents.Pdf.Objects;

namespace Radzen.Documents.Pdf;


/// <summary>
/// An axis-aligned rectangle defined by its top-left corner, width and height.
/// </summary>
/// <remarks>
/// Initializes a new <see cref="Rect"/>.
/// </remarks>
public readonly struct Rect(double x, double y, double width, double height) : IEquatable<Rect>
{

    /// <summary>Gets the X coordinate of the top-left corner.</summary>
    public double X { get; } = x;

    /// <summary>Gets the Y coordinate of the top-left corner.</summary>
    public double Y { get; } = y;

    /// <summary>Gets the width.</summary>
    public double Width { get; } = width;

    /// <summary>Gets the height.</summary>
    public double Height { get; } = height;

    /// <summary>Gets the left edge (equal to <see cref="X"/>).</summary>
    public double Left => X;

    /// <summary>Gets the top edge (equal to <see cref="Y"/>).</summary>
    public double Top => Y;

    /// <summary>Gets the right edge (<see cref="X"/> + <see cref="Width"/>).</summary>
    public double Right => X + Width;

    /// <summary>Gets the bottom edge (<see cref="Y"/> + <see cref="Height"/>).</summary>
    public double Bottom => Y + Height;

    /// <summary>
    /// Determines whether two rectangles are equal.
    /// </summary>
    public static bool operator ==(Rect left, Rect right) => left.Equals(right);

    /// <summary>
    /// Determines whether two rectangles are not equal.
    /// </summary>
    public static bool operator !=(Rect left, Rect right) => !left.Equals(right);

    /// <inheritdoc/>
    public bool Equals(Rect other)
        => X.Equals(other.X) && Y.Equals(other.Y) && Width.Equals(other.Width) && Height.Equals(other.Height);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Rect other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(X, Y, Width, Height);
}

/// <summary>How <see cref="PdfRects.Read"/> answers a missing, short or non-numeric /Rect array.</summary>
internal readonly struct RectPolicy
{
    private RectPolicy(string? missingMessage, string? nonNumericMessage, double fallbackWidth, double fallbackHeight)
    {
        MissingMessage = missingMessage;
        NonNumericMessage = nonNumericMessage;
        FallbackWidth = fallbackWidth;
        FallbackHeight = fallbackHeight;
    }

    public string? MissingMessage { get; }

    public string? NonNumericMessage { get; }

    public double FallbackWidth { get; }

    public double FallbackHeight { get; }

    public bool Throws => MissingMessage is not null;

    /// <summary>Requires exactly four resolvable numbers, throwing <see cref="DocumentParseException"/> otherwise.</summary>
    public static RectPolicy Strict(string missingMessage, string nonNumericMessage)
        => new(missingMessage, nonNumericMessage, 0, 0);

    /// <summary>Reads a missing or short array as an empty rectangle, and any non-numeric coordinate as zero.</summary>
    public static RectPolicy ZeroFallback { get; } = new(null, null, 0, 0);

    /// <summary>Reads a missing or short array as the given size at the origin, and any non-numeric coordinate as zero.</summary>
    public static RectPolicy DefaultSize(double width, double height) => new(null, null, width, height);
}

/// <summary>Reads a PDF /Rect array into PDF user space (Y-up, bottom-left origin).</summary>
internal static class PdfRects
{
    // A legal /Rect may state its corners in either order and may hold indirect references
    // (ISO 32000-1 7.9.5), so each coordinate is resolved and the result normalised.
    public static TextBounds Read(DocumentReader reader, ArrayObject? value, RectPolicy policy)
    {
        if (value is null || value.Count < 4 || (policy.Throws && value.Count != 4))
        {
            return policy.Throws
                ? throw new DocumentParseException(policy.MissingMessage!, -1)
                : new TextBounds(0, 0, policy.FallbackWidth, policy.FallbackHeight);
        }

        var corners = new double[4];
        for (var i = 0; i < corners.Length; i++)
        {
            corners[i] = reader.AsNumber(value[i]) switch
            {
                { } number => number,
                null when policy.Throws => throw new DocumentParseException(policy.NonNumericMessage!, -1),
                null => 0.0,
            };
        }

        return new TextBounds(
            Math.Min(corners[0], corners[2]),
            Math.Min(corners[1], corners[3]),
            Math.Max(corners[0], corners[2]),
            Math.Max(corners[1], corners[3]));
    }
}
