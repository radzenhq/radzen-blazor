using System;

namespace Radzen.Documents.Pdf;

/// <summary>Represents a point in PDF page coordinates.</summary>
/// <param name="x">The horizontal coordinate in points.</param>
/// <param name="y">The vertical coordinate in points.</param>
public readonly struct AnnotationPoint(double x, double y) : IEquatable<AnnotationPoint>
{
    /// <summary>Gets the horizontal coordinate in points.</summary>
    public double X { get; } = x;

    /// <summary>Gets the vertical coordinate in points.</summary>
    public double Y { get; } = y;

    /// <inheritdoc />
    public bool Equals(AnnotationPoint other) => X.Equals(other.X) && Y.Equals(other.Y);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is AnnotationPoint other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(X, Y);

    /// <summary>Determines whether two points are equal.</summary>
    public static bool operator ==(AnnotationPoint left, AnnotationPoint right) => left.Equals(right);

    /// <summary>Determines whether two points are different.</summary>
    public static bool operator !=(AnnotationPoint left, AnnotationPoint right) => !left.Equals(right);
}
