using System;

namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Represents a reference to a column in a spreadsheet.
/// </summary>
public readonly struct ColumnRef(int column) : IEquatable<ColumnRef>
{
    /// <summary>
    /// Returns the column index for the column reference.
    /// The column index is zero-based, so the first column (A) corresponds to 0.
    /// </summary>
    public int Column { get; } = column;

    /// <summary>
    /// Checks if the column reference is equal to another column reference.
    /// </summary>
    public bool Equals(ColumnRef other) => Column == other.Column;

    /// <summary>
    /// Returns a string representation of the column reference in A1 notation.
    /// For example, if the column reference is (0), it returns "A:A";
    /// </summary>
    public override string ToString()
    {
        var address = ToString(Column);

        return $"{address}:{address}";
    }

    /// <summary>
    /// Converts a zero-based column index to its corresponding column letter(s) in A1 notation.
    /// For example, 0 corresponds to "A", 1 to "B",
    /// 26 to "AA", 27 to "AB", and so on.
    /// </summary>
    /// <param name="column">The zero-based column index.</param>
    /// <returns>A string representing the column in A1 notation.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the column index is negative.</exception>
    public static string ToString(int column)
    {
        if (column < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(column), "Column index must be non-negative.");
        }

        var result = string.Empty;

        column++;

        while (column > 0)
        {
            column--;
            result = (char)('A' + column % 26) + result;
            column /= 26;
        }

        return result;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is ColumnRef address && Equals(address);

    /// <inheritdoc/>
    public override int GetHashCode() => Column.GetHashCode();

    /// <summary>
    /// Checks if the column reference is equal to another column reference.
    /// </summary>
    public static bool operator ==(ColumnRef left, ColumnRef right) => left.Equals(right);

    /// <summary>
    /// Checks if the column reference is not equal to another column reference.
    /// </summary>
    public static bool operator !=(ColumnRef left, ColumnRef right) => !left.Equals(right);
}