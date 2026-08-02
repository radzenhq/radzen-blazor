using System;
using Radzen.Documents.Core;

namespace Radzen.Documents.Pdf;

/// <summary>
/// An axis-aligned rectangle in PDF user space (Y-up, bottom-left origin), given by its
/// edges.
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

    internal bool IsFiniteAndPositive
        => double.IsFinite(Left) && double.IsFinite(Bottom) && double.IsFinite(Right) && double.IsFinite(Top)
            && Width > 0 && Height > 0;

    /// <summary>Creates a rectangle from its lower-left corner and a size.</summary>
    public static PdfRect FromSize(double left, double bottom, double width, double height)
        => new(left, bottom, left + width, bottom + height);

    internal static PdfRect FromLayout(Rect rect, double pageHeight)
        => new(rect.Left, pageHeight - rect.Bottom, rect.Right, pageHeight - rect.Top);

    internal Rect ToLayout(double pageHeight) => new(Left, pageHeight - Top, Width, Height);

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

    internal static PdfRect Normalize(double[] corners)
        => new(
            Math.Min(corners[0], corners[2]),
            Math.Min(corners[1], corners[3]),
            Math.Max(corners[0], corners[2]),
            Math.Max(corners[1], corners[3]));
}

internal struct PdfRectBounds
{
    private double left;
    private double bottom;
    private double right;
    private double top;

    public bool HasPoint { get; private set; }

    public void Include(double x, double y)
    {
        if (!HasPoint)
        {
            left = right = x;
            bottom = top = y;
            HasPoint = true;
            return;
        }

        left = Math.Min(left, x);
        right = Math.Max(right, x);
        bottom = Math.Min(bottom, y);
        top = Math.Max(top, y);
    }

    public readonly PdfRect ToRect() => new(left, bottom, right, top);

    public readonly PdfRect? ToRectOrNull() => HasPoint ? new PdfRect(left, bottom, right, top) : null;
}
