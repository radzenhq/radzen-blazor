using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Emit;

// The one 'interleave tables and boxes by their shared Order' contract. The section body,
// a table cell and an overlay container all consume it, so the paint z-order of mixed
// tables and containers is decided here once instead of drifting between the three.
internal static class OrderedMerge
{
    public static OrderedCursor<TTable, TBox> ByOrder<TTable, TBox>(
        IReadOnlyList<TTable> tables,
        Func<TTable, int> tableOrder,
        IReadOnlyList<TBox> boxes,
        Func<TBox, int> boxOrder)
        => new(tables, tableOrder, boxes, boxOrder);
}

// Two-pointer merge over two Order-sorted sequences. Callers index their own list with
// TableIndex/BoxIndex, so nothing is copied out of a struct list to walk it.
internal struct OrderedCursor<TTable, TBox>(
    IReadOnlyList<TTable> tables,
    Func<TTable, int> tableOrder,
    IReadOnlyList<TBox> boxes,
    Func<TBox, int> boxOrder)
{
    private int t;
    private int b;

    public bool IsTable { get; private set; }

    public int TableIndex { get; private set; }

    public int BoxIndex { get; private set; }

    // Equal Order resolves to the table, so a table and a box placed at the same Order
    // always paint table-first wherever they are nested.
    public bool MoveNext()
    {
        if (t >= tables.Count && b >= boxes.Count)
        {
            return false;
        }

        IsTable = b >= boxes.Count || (t < tables.Count && tableOrder(tables[t]) <= boxOrder(boxes[b]));
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
