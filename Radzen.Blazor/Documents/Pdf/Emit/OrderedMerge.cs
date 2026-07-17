using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Emit;

internal static class OrderedMerge
{
    public static OrderedCursor<TTable, TBox> ByOrder<TTable, TBox>(
        IReadOnlyList<TTable> tables,
        Func<TTable, int> tableOrder,
        IReadOnlyList<TBox> boxes,
        Func<TBox, int> boxOrder)
        => new(tables, tableOrder, boxes, boxOrder);
}

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
