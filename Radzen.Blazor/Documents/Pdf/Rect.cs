using System;
using Radzen.Documents.Pdf.Objects;

namespace Radzen.Documents.Pdf;


/// <summary>
/// An axis-aligned rectangle defined by its top-left corner, width and height, in layout
/// space (Y grows downwards). PDF user space is <see cref="PdfRect"/>.
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

/// <summary>How <see cref="PdfRect.Read"/> answers a missing, short or non-numeric /Rect array.</summary>
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

/// <summary>
/// An axis-aligned rectangle in PDF user space (Y-up, bottom-left origin), given by its
/// edges. Layout space is <see cref="Rect"/>; convert with <see cref="FromLayout"/> or
/// <see cref="ToLayout"/>, which need the height of the page being measured against.
/// </summary>
/// <param name="left">The minimum horizontal coordinate.</param>
/// <param name="bottom">The minimum vertical coordinate.</param>
/// <param name="right">The maximum horizontal coordinate.</param>
/// <param name="top">The maximum vertical coordinate.</param>
public readonly struct PdfRect(double left, double bottom, double right, double top) : IEquatable<PdfRect>
{
    /// <summary>Gets the minimum horizontal coordinate.</summary>
    public double Left { get; } = left;

    /// <summary>Gets the minimum vertical coordinate.</summary>
    public double Bottom { get; } = bottom;

    /// <summary>Gets the maximum horizontal coordinate.</summary>
    public double Right { get; } = right;

    /// <summary>Gets the maximum vertical coordinate.</summary>
    public double Top { get; } = top;

    /// <summary>Gets the width.</summary>
    public double Width => Right - Left;

    /// <summary>Gets the height.</summary>
    public double Height => Top - Bottom;

    /// <summary>Creates a rectangle from its lower-left corner and a size.</summary>
    public static PdfRect FromSize(double left, double bottom, double width, double height)
        => new(left, bottom, left + width, bottom + height);

    /// <summary>Converts a layout rectangle on a page of the given height into PDF user space.</summary>
    public static PdfRect FromLayout(Rect rect, double pageHeight)
        => new(rect.Left, pageHeight - rect.Bottom, rect.Right, pageHeight - rect.Top);

    /// <summary>Converts this rectangle into layout space on a page of the given height.</summary>
    public Rect ToLayout(double pageHeight) => new(Left, pageHeight - Top, Width, Height);

    /// <summary>Determines whether two rectangles are equal.</summary>
    public static bool operator ==(PdfRect left, PdfRect right) => left.Equals(right);

    /// <summary>Determines whether two rectangles are not equal.</summary>
    public static bool operator !=(PdfRect left, PdfRect right) => !left.Equals(right);

    /// <inheritdoc/>
    public bool Equals(PdfRect other)
        => Left.Equals(other.Left) && Bottom.Equals(other.Bottom) && Right.Equals(other.Right) && Top.Equals(other.Top);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is PdfRect other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Left, Bottom, Right, Top);

    // A legal /Rect may state its corners in either order and may hold indirect references
    // (ISO 32000-1 7.9.5), so each coordinate is resolved and the result normalised.
    internal static PdfRect Read(DocumentReader reader, ArrayObject? value, RectPolicy policy)
    {
        if (value is null || value.Count < 4 || (policy.Throws && value.Count != 4))
        {
            return policy.Throws
                ? throw new DocumentParseException(policy.MissingMessage!, -1)
                : FromSize(0, 0, policy.FallbackWidth, policy.FallbackHeight);
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

        return new PdfRect(
            Math.Min(corners[0], corners[2]),
            Math.Min(corners[1], corners[3]),
            Math.Max(corners[0], corners[2]),
            Math.Max(corners[1], corners[3]));
    }
}
