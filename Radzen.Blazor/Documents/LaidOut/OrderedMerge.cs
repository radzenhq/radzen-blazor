using System;
using System.Collections.Immutable;

namespace Radzen.Documents.LaidOut;

internal static class OrderedMerge
{
    public static OrderedCursor<TTable, TBox> ByOrder<TTable, TBox>(
        ImmutableArray<TTable> tables,
        Func<TTable, int> tableOrder,
        ImmutableArray<TBox> boxes,
        Func<TBox, int> boxOrder)
        => new(tables, tableOrder, boxes, boxOrder);
}

internal struct OrderedCursor<TTable, TBox>(
    ImmutableArray<TTable> tables,
    Func<TTable, int> tableOrder,
    ImmutableArray<TBox> boxes,
    Func<TBox, int> boxOrder)
{
    private int t;
    private int b;

    public bool IsTable { get; private set; }

    public int TableIndex { get; private set; }

    public int BoxIndex { get; private set; }

    public bool MoveNext()
    {
        if (t >= tables.Length && b >= boxes.Length)
        {
            return false;
        }

        IsTable = b >= boxes.Length || (t < tables.Length && tableOrder(tables[t]) <= boxOrder(boxes[b]));
        if (IsTable)
        {
            TableIndex = t++;
        }
        else
        {
            BoxIndex = b++;
        }

        return true;
    }
}
