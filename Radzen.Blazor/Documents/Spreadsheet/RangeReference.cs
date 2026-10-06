#nullable enable

namespace Radzen.Documents.Spreadsheet;

internal sealed class RangeReference(FormulaEvaluator evaluator, Worksheet worksheet, int startRow, int startColumn, int rows, int columns, int logicalRows, int logicalColumns, bool isCell)
{
    public Worksheet Worksheet { get; } = worksheet;

    public int StartRow { get; } = startRow;

    public int StartColumn { get; } = startColumn;

    public int Rows { get; } = rows;

    public int Columns { get; } = columns;

    public int LogicalRows { get; } = logicalRows;

    public int LogicalColumns { get; } = logicalColumns;

    public bool IsCell { get; } = isCell;

    public CellData this[int row, int column]
    {
        get
        {
            var sheetRow = StartRow + row;
            var sheetColumn = StartColumn + column;

            if (sheetRow >= Worksheet.RowCount || sheetColumn >= Worksheet.ColumnCount)
            {
                return CellData.Empty;
            }

            if (!Worksheet.Cells.TryGet(sheetRow, sheetColumn, out var cell))
            {
                return new CellData(null);
            }

            return evaluator.ReadCell(cell);
        }
    }

    public CellData this[int index] => this[index / Columns, index % Columns];
}
