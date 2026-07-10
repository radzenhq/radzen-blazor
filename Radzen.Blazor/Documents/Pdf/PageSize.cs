using System;

namespace Radzen.Documents.Pdf;

#nullable enable

/// <summary>
/// The width and height of a page.
/// </summary>
public readonly struct PageSize : IEquatable<PageSize>
{
    /// <summary>
    /// Initializes a new <see cref="PageSize"/>.
    /// </summary>
    public PageSize(Unit width, Unit height)
    {
        Width = width;
        Height = height;
    }

    /// <summary>Gets the page width.</summary>
    public Unit Width { get; }

    /// <summary>Gets the page height.</summary>
    public Unit Height { get; }

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
