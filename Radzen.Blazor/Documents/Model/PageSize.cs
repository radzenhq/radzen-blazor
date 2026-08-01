using System;
using Radzen.Documents.Core;

namespace Radzen.Documents;


/// <summary>
/// The width and height of a page.
/// </summary>
/// <remarks>
/// Initializes a new <see cref="PageSize"/>.
/// </remarks>
/// <exception cref="ArgumentOutOfRangeException">
/// <paramref name="width"/> or <paramref name="height"/> is relative or is not greater than zero.
/// </exception>
public readonly struct PageSize(Unit width, Unit height) : IEquatable<PageSize>
{

    /// <summary>Gets the page width.</summary>
    public Unit Width { get; } = AuthoredNumber.AbsolutePositive(width, "PageSize.Width");

    /// <summary>Gets the page height.</summary>
    public Unit Height { get; } = AuthoredNumber.AbsolutePositive(height, "PageSize.Height");

    internal (Unit Width, Unit Height) Effective(PageOrientation orientation)
        => orientation == PageOrientation.Landscape ? (Height, Width) : (Width, Height);

    /// <summary>
    /// Determines whether two page sizes are equal.
    /// </summary>
    public static bool operator ==(PageSize left, PageSize right) => left.Equals(right);

    /// <summary>
    /// Determines whether two page sizes are not equal.
    /// </summary>
    public static bool operator !=(PageSize left, PageSize right) => !left.Equals(right);

    /// <inheritdoc/>
    public bool Equals(PageSize other) => Width.Equals(other.Width) && Height.Equals(other.Height);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is PageSize other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Width, Height);
}
