using System.Collections.Generic;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Holds per-sheet UI state (undo/redo history, rendering layout) that is not part of the document model.
/// </summary>
public class SheetView
{
    /// <summary>
    /// Gets the document sheet this view wraps.
    /// </summary>
    public Worksheet Worksheet { get; }

    /// <summary>
    /// Gets the per-sheet undo/redo stack.
    /// </summary>
    public UndoRedoStack Commands { get; } = new();

    /// <summary>
    /// Gets or sets the header offset for rows (height of column headers in pixels).
    /// </summary>
    public double RowHeaderOffset { get; set; } = 24;

    /// <summary>
    /// Gets or sets the header offset for columns (width of row headers in pixels).
    /// </summary>
    public double ColumnHeaderOffset { get; set; } = 100;

    /// <summary>
    /// Initializes a new instance of the <see cref="SheetView"/> class.
    /// Injects its own UndoRedoStack into the sheet so callers can use Worksheet.Commands transparently.
    /// </summary>
    public SheetView(Worksheet sheet)
    {
        Worksheet = sheet ?? throw new System.ArgumentNullException(nameof(sheet));
        sheet.Commands = Commands;
    }

    /// <summary>
    /// Gets the visible row index range for the given scroll position and viewport height.
    /// Accounts for the row header offset.
    /// </summary>
    public IndexRange GetRowRange(double start, double end, bool includeFrozen = false)
    {
        return Worksheet.Rows.GetIndexRange(start - RowHeaderOffset, end - RowHeaderOffset, includeFrozen);
    }

    /// <summary>
    /// Gets the visible column index range for the given scroll position and viewport width.
    /// Accounts for the column header offset.
    /// </summary>
    public IndexRange GetColumnRange(double start, double end, bool includeFrozen = false)
    {
        return Worksheet.Columns.GetIndexRange(start - ColumnHeaderOffset, end - ColumnHeaderOffset, includeFrozen);
    }

    /// <summary>
    /// Gets the pixel range for the specified row indices, offset by the row header height.
    /// </summary>
    public PixelRange GetRowPixelRange(int startIndex, int endIndex)
    {
        var range = Worksheet.Rows.GetPixelRange(startIndex, endIndex);
        return new PixelRange(range.Start + RowHeaderOffset, range.End + RowHeaderOffset);
    }

    /// <summary>
    /// Gets the pixel range for a single row index, offset by the row header height.
    /// </summary>
    public PixelRange GetRowPixelRange(int index)
    {
        var range = Worksheet.Rows.GetPixelRange(index);
        return new PixelRange(range.Start + RowHeaderOffset, range.End + RowHeaderOffset);
    }

    /// <summary>
    /// Gets the pixel range for the specified column indices, offset by the column header width.
    /// </summary>
    public PixelRange GetColumnPixelRange(int startIndex, int endIndex)
    {
        var range = Worksheet.Columns.GetPixelRange(startIndex, endIndex);
        return new PixelRange(range.Start + ColumnHeaderOffset, range.End + ColumnHeaderOffset);
    }

    /// <summary>
    /// Gets the pixel range for a single column index, offset by the column header width.
    /// </summary>
    public PixelRange GetColumnPixelRange(int index)
    {
        var range = Worksheet.Columns.GetPixelRange(index);
        return new PixelRange(range.Start + ColumnHeaderOffset, range.End + ColumnHeaderOffset);
    }

    /// <summary>
    /// Gets the total scrollable height including the row header offset.
    /// </summary>
    public double TotalHeight => Worksheet.Rows.Total + RowHeaderOffset;

    /// <summary>
    /// Gets the total scrollable width including the column header offset.
    /// </summary>
    public double TotalWidth => Worksheet.Columns.Total + ColumnHeaderOffset;

    /// <summary>
    /// Splits a range into regions based on frozen pane boundaries for rendering.
    /// </summary>
    public IEnumerable<RangeInfo> GetRanges(RangeRef range)
    {
        if (range == RangeRef.Invalid)
        {
            yield break;
        }

        var frozenRows = Worksheet.Rows.Frozen;
        var frozenColumns = Worksheet.Columns.Frozen;

        var bottomRightRange = new RangeRef(new CellRef(frozenRows, frozenColumns), new CellRef(Worksheet.RowCount - 1, Worksheet.ColumnCount - 1));

        if (range.Overlaps(bottomRightRange))
        {
            yield return new RangeInfo
            {
                Range = range.Intersection(bottomRightRange),
                FrozenRow = false,
                FrozenColumn = false,
                Top = range.Start.Row >= frozenRows,
                Left = range.Start.Column >= frozenColumns,
                Bottom = true,
                Right = true
            };
        }

        var topLeftRange = frozenRows > 0 && frozenColumns > 0
            ? new RangeRef(new CellRef(0, 0), new CellRef(frozenRows - 1, frozenColumns - 1))
            : RangeRef.Invalid;

        if (range.Overlaps(topLeftRange))
        {
            yield return new RangeInfo
            {
                Range = range.Intersection(topLeftRange),
                FrozenRow = true,
                FrozenColumn = true,
                Top = true,
                Left = true,
                Bottom = range.End.Row < frozenRows,
                Right = range.End.Column < frozenColumns
            };
        }

        var topRightRange = frozenRows > 0
            ? new RangeRef(new CellRef(0, frozenColumns), new CellRef(frozenRows - 1, Worksheet.ColumnCount - 1))
            : RangeRef.Invalid;

        if (range.Overlaps(topRightRange))
        {
            yield return new RangeInfo
            {
                Range = range.Intersection(topRightRange),
                FrozenRow = true,
                FrozenColumn = false,
                Top = true,
                Left = range.Start.Column >= frozenColumns,
                Bottom = range.End.Row < frozenRows,
                Right = true
            };
        }

        var bottomLeftRange = frozenColumns > 0
            ? new RangeRef(new CellRef(frozenRows, 0), new CellRef(Worksheet.RowCount - 1, frozenColumns - 1))
            : RangeRef.Invalid;

        if (range.Overlaps(bottomLeftRange))
        {
            yield return new RangeInfo
            {
                Range = range.Intersection(bottomLeftRange),
                FrozenRow = false,
                FrozenColumn = true,
                Top = range.Start.Row >= frozenRows,
                Left = true,
                Bottom = true,
                Right = range.End.Column < frozenColumns
            };
        }
    }
}
