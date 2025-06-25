using System;
using System.Collections.Generic;

namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Represents a range of cells in a spreadsheet.
/// </summary>
public readonly struct RangeRef : IEquatable<RangeRef>
{
    /// <summary>
    /// Represents an invalid range reference, which is used to indicate that a range is not defined or does not exist.
    /// </summary>
    public static RangeRef Invalid { get; } = new(CellRef.Invalid, CellRef.Invalid);

    /// <summary>
    /// Initializes a new instance of the <see cref="RangeRef"/> struct with the specified start and end cell references.
    /// </summary>
    public RangeRef(CellRef start, CellRef end)
    {
        var minRow = Math.Min(start.Row, end.Row);
        var maxRow = Math.Max(start.Row, end.Row);
        var minCol = Math.Min(start.Column, end.Column);
        var maxCol = Math.Max(start.Column, end.Column);

        Start = new CellRef(minRow, minCol);
        End = new CellRef(maxRow, maxCol);
    }

    /// <summary>
    /// Gets the number of rows and columns in the range.
    /// </summary>
    public int Rows => End.Row - Start.Row + 1;
    /// <summary>
    /// Gets the number of columns in the range.
    /// </summary>
    public int Columns => End.Column - Start.Column + 1;

    /// <summary>
    /// Gets the starting cell reference of the range.
    /// </summary>
    public CellRef Start { get; }

    /// <summary>
    /// Gets the ending cell reference of the range.
    /// </summary>
    public CellRef End { get; }

    /// <summary>
    /// Returns a collection of cell references that represent all the cells in the range.
    /// </summary>
    public IEnumerable<CellRef> GetCells()
    {
        for (var row = Start.Row; row <= End.Row; row++)
        {
            for (var col = Start.Column; col <= End.Column; col++)
            {
                yield return new CellRef(row, col);
            }
        }
    }

    /// <summary>
    /// Checks if this range overlaps with another range.
    /// </summary>
    public bool Overlaps(RangeRef other)
    {
        if (other == Invalid || this == Invalid)
        {
            return false;
        }

        return Start.Row <= other.End.Row && End.Row >= other.Start.Row &&
               Start.Column <= other.End.Column && End.Column >= other.Start.Column;
    }

    /// <summary>
    /// Calculates the intersection of this range with another range.
    /// </summary>
    public RangeRef Intersection(RangeRef other)
    {
        var startRow = Math.Max(Start.Row, other.Start.Row);
        var endRow = Math.Min(End.Row, other.End.Row);
        var startCol = Math.Max(Start.Column, other.Start.Column);
        var endCol = Math.Min(End.Column, other.End.Column);

        return new RangeRef(new CellRef(startRow, startCol), new CellRef(endRow, endCol));
    }

    /// <summary>
    /// Checks if this range contains a specific row address.
    /// </summary>
    /// <param name="address"></param>
    /// <returns></returns>
    public bool Contains(RowRef address) => address.Row >= Start.Row && address.Row <= End.Row;

    /// <summary>
    /// Checks if this range contains a specific column address.
    /// </summary>
    /// <param name="address"></param>
    /// <returns></returns>
    public bool Contains(ColumnRef address) => address.Column >= Start.Column && address.Column <= End.Column;

    /// <summary>
    /// Checks if this range contains a specific row and column.
    /// </summary>
    public bool Contains(int row, int column) => row >= Start.Row && row <= End.Row && column >= Start.Column && column <= End.Column;

    /// <summary>
    /// Checks if this range contains a specific cell address.
    /// </summary>
    public bool Contains(CellRef address) => Contains(address.Row, address.Column);

    /// <summary>
    /// Checks if this range is collapsed, meaning the start and end cell references are the same.
    /// </summary>
    public bool Collapsed => Start.Equals(End);

    /// <summary>
    /// Creates a new <see cref="RangeRef"/> instance from two cell references, representing the start and end of the range.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public static RangeRef FromCells(CellRef start, CellRef end) => new(start, end);

    /// <summary>
    /// Creates a new <see cref="RangeRef"/> instance from a single cell reference, representing a range that starts and ends at the same cell.
    /// </summary>
    /// <param name="cell"></param>
    /// <returns></returns>
    public static RangeRef FromCell(CellRef cell) => new(cell, cell);

    /// <summary>
    /// Parses a string representation of a range into a <see cref="RangeRef"/> instance.
    /// </summary>
    /// <param name="range"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static RangeRef Parse(string range)
    {
        if (range.IndexOf(':') < 0)
        {
            return new RangeRef(CellRef.Parse(range), CellRef.Parse(range));
        }

        var parts = range.Split(':');

        if (parts.Length != 2)
        {
            throw new ArgumentException($"Invalid range format: {range}");
        }

        return new RangeRef(CellRef.Parse(parts[0]), CellRef.Parse(parts[1]));
    }

    /// <summary>
    /// Returns a string representation of the range in A1 notation (e.g., "A1:B2").
    /// </summary>
    /// <returns></returns>
    public override string ToString() => $"{Start}:{End}";

    /// <summary>
    /// Checks if this <see cref="RangeRef"/> instance is equal to another <see cref="RangeRef"/> instance.
    /// </summary>
    public bool Equals(RangeRef other) => Start.Equals(other.Start) && End.Equals(other.End);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is RangeRef range && Equals(range);

    /// <summary>
    /// Checks if two <see cref="RangeRef"/> instances are equal.
    /// </summary>
    public static bool operator ==(RangeRef left, RangeRef right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Checks if two <see cref="RangeRef"/> instances are not equal.
    /// </summary>
    public static bool operator !=(RangeRef left, RangeRef right)
    {
        return !(left == right);
    }

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Start, End);
} 