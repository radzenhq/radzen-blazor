using System;

namespace Radzen.Documents.Spreadsheet;

#nullable enable

internal readonly record struct AxisShift(int Index, int Count, bool IsRow)
{
    private RangeKind Axis => IsRow ? RangeKind.Rows : RangeKind.Columns;

    private bool IsDelete => Count < 0;

    private int Position(CellRef address) => IsRow ? address.Row : address.Column;

    private CellRef Move(CellRef address, int position) => IsRow
        ? new CellRef(position, address.Column) { Worksheet = address.Worksheet }
        : new CellRef(address.Row, position) { Worksheet = address.Worksheet };

    public int Map(int position) => IsDelete
        ? (position > Index ? position + Count : position)
        : (position >= Index ? position + Count : position);

    public CellRef AdjustCell(FormulaToken token)
    {
        var address = token.Address;
        var position = Position(address);

        if (IsDelete && position == Index)
        {
            return CellRef.Invalid with { Worksheet = address.Worksheet };
        }

        return Move(address, Map(position));
    }

    public (CellRef Start, CellRef End)? AdjustRange(FormulaToken start, FormulaToken end, RangeKind kind)
    {
        if (kind != RangeKind.Cells && kind != Axis)
        {
            return (start.Address, end.Address);
        }

        var startPosition = Position(start.Address);
        var endPosition = Position(end.Address);
        var low = Math.Min(startPosition, endPosition);
        var high = Math.Max(startPosition, endPosition);

        if (IsDelete && low == Index && high == Index)
        {
            return null;
        }

        var movedLow = Map(low);
        var movedHigh = IsDelete && high == Index ? high + Count : Map(high);

        return startPosition <= endPosition
            ? (Move(start.Address, movedLow), Move(end.Address, movedHigh))
            : (Move(start.Address, movedHigh), Move(end.Address, movedLow));
    }
}
