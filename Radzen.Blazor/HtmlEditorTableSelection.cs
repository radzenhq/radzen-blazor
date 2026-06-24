namespace Radzen;

/// <summary>
/// Describes the current table selection inside <see cref="Radzen.Blazor.RadzenHtmlEditor" />.
/// </summary>
public class HtmlEditorTableSelection
{
    /// <summary>
    /// Gets or sets a value indicating whether the current selection is inside a table.
    /// </summary>
    public bool InTable { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the current selection is inside a header cell.
    /// </summary>
    public bool InHeader { get; set; }

    /// <summary>
    /// Gets or sets the zero-based row index within the body section.
    /// </summary>
    public int RowIndex { get; set; } = -1;

    /// <summary>
    /// Gets or sets the zero-based column index.
    /// </summary>
    public int ColumnIndex { get; set; } = -1;

    /// <summary>
    /// Gets or sets the number of body rows.
    /// </summary>
    public int Rows { get; set; }

    /// <summary>
    /// Gets or sets the number of columns.
    /// </summary>
    public int Columns { get; set; }

    /// <summary>
    /// Gets or sets the table width.
    /// </summary>
    public string? Width { get; set; }

    /// <summary>
    /// Gets or sets the table border.
    /// </summary>
    public int Border { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the current table contains a header row.
    /// </summary>
    public bool HeaderRow { get; set; }

    /// <summary>
    /// Gets or sets the current cell column span.
    /// </summary>
    public int ColSpan { get; set; } = 1;

    /// <summary>
    /// Gets or sets the current cell row span.
    /// </summary>
    public int RowSpan { get; set; } = 1;

    /// <summary>
    /// Gets or sets a value indicating whether the current cell can be merged with the cell to the right.
    /// </summary>
    public bool CanMergeRight { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the current cell can be merged with the cell below.
    /// </summary>
    public bool CanMergeDown { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the current cell can be split.
    /// </summary>
    public bool CanSplit { get; set; }

    /// <summary>
    /// Gets or sets the current column width.
    /// </summary>
    public string? ColumnWidth { get; set; }

    /// <summary>
    /// Gets or sets the current column width in pixels.
    /// </summary>
    public int? ColumnWidthPx { get; set; }

    /// <summary>
    /// Gets or sets the current cell background color.
    /// </summary>
    public string? CellBackground { get; set; }

    /// <summary>
    /// Gets or sets the current cell padding.
    /// </summary>
    public string? CellPadding { get; set; }

    /// <summary>
    /// Gets or sets the current cell padding in pixels.
    /// </summary>
    public int? CellPaddingPx { get; set; }

    /// <summary>
    /// Gets or sets the current cell horizontal alignment.
    /// </summary>
    public string? CellTextAlign { get; set; }

    /// <summary>
    /// Gets or sets the current cell vertical alignment.
    /// </summary>
    public string? CellVerticalAlign { get; set; }

    /// <summary>
    /// Gets or sets the current cell border style.
    /// </summary>
    public string? CellBorder { get; set; }

    /// <summary>
    /// Gets or sets the current cell border style preset.
    /// </summary>
    public string? BorderStyle { get; set; }

    /// <summary>
    /// Gets or sets the current cell border width in pixels.
    /// </summary>
    public int? BorderWidthPx { get; set; }

    /// <summary>
    /// Gets or sets the current cell border color.
    /// </summary>
    public string? BorderColor { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the top border is present.
    /// </summary>
    public bool BorderTop { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the right border is present.
    /// </summary>
    public bool BorderRight { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the bottom border is present.
    /// </summary>
    public bool BorderBottom { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the left border is present.
    /// </summary>
    public bool BorderLeft { get; set; }
}
