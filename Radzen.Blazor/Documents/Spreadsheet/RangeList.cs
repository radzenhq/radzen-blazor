#nullable enable

using System;
using System.Collections.Generic;

namespace Radzen.Documents.Spreadsheet;

internal class RangeList : List<CellData>
{
    private readonly Worksheet sheet;
    internal Worksheet Worksheet => sheet;
    public int Rows { get; }

    public int Columns { get; }

    public int StartRow { get; }

    public int StartColumn { get; }

    public RangeList(int rows, int columns, int startRow, int startColumn, Worksheet sheet)
    {
        Rows = rows;
        Columns = columns;
        StartRow = startRow;
        StartColumn = startColumn;
        this.sheet = sheet;
    }

    public bool IsRowHiddenAt(int itemIndex)
    {
        var rowOffset = itemIndex / Math.Max(1, Columns);
        return sheet.Rows.IsHidden(StartRow + rowOffset);
    }
}