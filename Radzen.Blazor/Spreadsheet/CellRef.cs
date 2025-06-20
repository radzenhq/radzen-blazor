using System;

namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Represents a reference to a cell in A1 notation e.g. "A1", "B2", etc.
/// The row and column are zero-based indices, so "A1" corresponds to (0, 0) and "B2" corresponds to (1, 1).
/// This struct is immutable and provides methods for parsing, comparing, and converting to string representation.
/// </summary>
public readonly struct CellRef(int row, int column) : IEquatable<CellRef>
{
    /// <summary>
    /// Represents an invalid cell reference with row and column set to -1.
    /// </summary>
    public static CellRef Invalid { get; } = new(-1, -1);
    
    /// <summary>
    /// Gets the row index of the cell reference.
    /// The row index is zero-based, so the first row (A1) corresponds to 0.
    /// </summary>
    public int Row { get; } = row;
    /// <summary>
    /// Gets the column index of the cell reference.
    /// The column index is zero-based, so the first column (A1) corresponds to
    /// </summary>
    public int Column { get; } = column;

    /// <inheritdoc/>
    public bool Equals(CellRef other) => Row == other.Row && Column == other.Column;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is CellRef other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Row, Column);

    /// <summary>
    /// Checks if the cell reference is equal to another cell reference.
    /// </summary>
    public static bool operator ==(CellRef left, CellRef right) => left.Equals(right);

    /// <summary>
    /// Checks if the cell reference is not equal to another cell reference.
    /// </summary>
    public static bool operator !=(CellRef left, CellRef right) => !left.Equals(right);

    /// <summary>
    /// Returns a string representation of the cell reference in A1 notation.
    /// For example, if the cell reference is (0, 0), it returns "A1";
    /// if the cell reference is (1, 1), it returns "B2".
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        var column = ColumnRef.ToString(Column);

        return $"{column}{Row + 1}";
    }

    /// <summary>
    /// Converts the cell reference to a range reference that includes the cell itself.
    /// </summary>
    public RangeRef ToRange() => new(this, this);

    /// <summary>
    /// Parses a string in A1 notation (e.g., "A1", "B2") into a CellRef instance.
    /// If the string is not a valid A1 notation, it throws an ArgumentException.
    /// </summary>
    /// <param name="index">The A1 notation string to parse.</param>
    /// <returns>A CellRef instance representing the parsed cell reference.</returns>
    /// <exception cref="ArgumentException">Thrown if the input string is not a valid A1 notation.</exception>
    public static CellRef Parse(string index)
    {
        if (!TryParse(index, out var result))
        {
            throw new ArgumentException($"Invalid A1 notation: {index}");
        }

        return result;
    }

    /// <summary>
    /// Swaps two CellRef instances if the first one is greater than the second one.
    /// This is useful for ensuring a consistent order when comparing or storing cell references.
    /// </summary>
    public static (CellRef, CellRef) Swap(CellRef a, CellRef b)
    {
        if (a.Row > b.Row || (a.Row == b.Row && a.Column > b.Column))
        {
            return (b, a);
        }

        return (a, b);
    }

    /// <summary>
    /// Attempts to parse a string in A1 notation into a CellRef instance.
    /// Returns true if the parsing is successful, otherwise false.
    /// </summary>
    /// <param name="index">The A1 notation string to parse.</param>
    /// <param name="result">The parsed CellRef instance if successful, otherwise default.</param>
    /// <returns>True if parsing was successful, otherwise false.</returns>
    public static bool TryParse(string index, out CellRef result)
    {
        result = default;

        if (string.IsNullOrEmpty(index))
        {
            return false;
        }

        var column = 0;
        var row = 0;
        var hasLetters = false;
        var hasNumbers = false;

        for (var i = 0; i < index.Length; i++)
        {
            var ch = index[i];

            if (ch >= '1' && ch <= '9')
            {
                hasNumbers = true;

                if (!int.TryParse(index[i..], out row))
                {
                    return false;
                }

                break;
            }

            if (!((ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z')))
            {
                return false;
            }

            hasLetters = true;
            column = column * 26 + ch - 'A' + 1;
        }

        if (!hasLetters || !hasNumbers)
        {
            return false;
        }

        result = new CellRef(row - 1, column - 1);
        return true;
    }
} 