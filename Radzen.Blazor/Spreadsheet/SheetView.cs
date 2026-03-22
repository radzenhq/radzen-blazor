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
    public Sheet Sheet { get; }

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
    /// </summary>
    public SheetView(Sheet sheet)
    {
        Sheet = sheet ?? throw new System.ArgumentNullException(nameof(sheet));
    }

    /// <summary>
    /// Gets the visible row index range for the given scroll position and viewport height.
    /// </summary>
    public IndexRange GetRowRange(double start, double end, bool includeFrozen = false)
    {
        return Sheet.Rows.GetIndexRange(start, end, includeFrozen);
    }

    /// <summary>
    /// Gets the visible column index range for the given scroll position and viewport width.
    /// </summary>
    public IndexRange GetColumnRange(double start, double end, bool includeFrozen = false)
    {
        return Sheet.Columns.GetIndexRange(start, end, includeFrozen);
    }

    /// <summary>
    /// Gets the pixel range for the specified row indices.
    /// </summary>
    public PixelRange GetRowPixelRange(int startIndex, int endIndex)
    {
        return Sheet.Rows.GetPixelRange(startIndex, endIndex);
    }

    /// <summary>
    /// Gets the pixel range for a single row index.
    /// </summary>
    public PixelRange GetRowPixelRange(int index)
    {
        return Sheet.Rows.GetPixelRange(index);
    }

    /// <summary>
    /// Gets the pixel range for the specified column indices.
    /// </summary>
    public PixelRange GetColumnPixelRange(int startIndex, int endIndex)
    {
        return Sheet.Columns.GetPixelRange(startIndex, endIndex);
    }

    /// <summary>
    /// Gets the pixel range for a single column index.
    /// </summary>
    public PixelRange GetColumnPixelRange(int index)
    {
        return Sheet.Columns.GetPixelRange(index);
    }

    /// <summary>
    /// Gets the total scrollable height including all visible rows and the header offset.
    /// </summary>
    public double TotalHeight => Sheet.Rows.Total;

    /// <summary>
    /// Gets the total scrollable width including all visible columns and the header offset.
    /// </summary>
    public double TotalWidth => Sheet.Columns.Total;
}
