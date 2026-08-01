using System;

namespace Radzen.Documents.Core;


/// <summary>
/// An axis-aligned rectangle defined by its top-left corner, width and height, in layout
/// space (Y grows downwards).
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
