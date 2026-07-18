using System;

namespace Radzen.Documents.Pdf;


/// <summary>
/// The width and height of a page.
/// </summary>
/// <remarks>
/// Initializes a new <see cref="PageSize"/>.
/// </remarks>
public readonly struct PageSize(Unit width, Unit height) : IEquatable<PageSize>
{

    /// <summary>Gets the page width.</summary>
    public Unit Width { get; } = width;

    /// <summary>Gets the page height.</summary>
    public Unit Height { get; } = height;

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
