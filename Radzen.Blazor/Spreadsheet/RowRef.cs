using System;

namespace Radzen.Blazor.Spreadsheet;

#nullable enable
/// <summary>
/// Represents a reference to a specific row in a spreadsheet.
/// </summary>
public readonly struct RowRef(int row) : IEquatable<RowRef>
{
    /// <summary>
    /// Returns the row index, which is zero-based.
    /// </summary>
    public int Row { get; } = row;

    /// <summary>
    /// Determines whether the current row reference is equal to another row reference.
    /// </summary>
    public bool Equals(RowRef other) => Row == other.Row;

    /// <summary>
    /// Returns the row in A1 notation, which is one-based e.g. "1:1" for the first row.
    /// </summary>
    public override string ToString() => $"{Row + 1}:{Row + 1}";

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is RowRef address && Equals(address);

    /// <inheritdoc/>
    public override int GetHashCode() => Row.GetHashCode();

    /// <summary>
    /// Checks if two <see cref="RowRef"/> instances are equal.
    /// </summary>
    public static bool operator ==(RowRef left, RowRef right) => left.Equals(right);

    /// <summary>
    /// Checks if two <see cref="RowRef"/> instances are not equal.
    /// </summary>
    public static bool operator !=(RowRef left, RowRef right) => !left.Equals(right);
}