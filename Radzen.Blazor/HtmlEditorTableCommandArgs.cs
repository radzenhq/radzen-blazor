namespace Radzen;

/// <summary>
/// Contains the arguments for HTML editor table commands.
/// </summary>
public class HtmlEditorTableCommandArgs
{
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
    /// Gets or sets the table border size.
    /// </summary>
    public int Border { get; set; } = 1;

    /// <summary>
    /// Gets or sets a value indicating whether the table has a header row.
    /// </summary>
    public bool HeaderRow { get; set; }

    /// <summary>
    /// Gets or sets the selected column width.
    /// </summary>
    public string? ColumnWidth { get; set; }

    /// <summary>
    /// Gets or sets the selected column width in pixels.
    /// </summary>
    public int? ColumnWidthPx { get; set; }

    /// <summary>
    /// Gets or sets the selected cell background color.
    /// </summary>
    public string? CellBackground { get; set; }

    /// <summary>
    /// Gets or sets the selected cell padding.
    /// </summary>
    public string? CellPadding { get; set; }

    /// <summary>
    /// Gets or sets the selected cell padding in pixels.
    /// </summary>
    public int? CellPaddingPx { get; set; }

    /// <summary>
    /// Gets or sets the selected cell horizontal alignment.
    /// </summary>
    public string? CellTextAlign { get; set; }

    /// <summary>
    /// Gets or sets the selected cell vertical alignment.
    /// </summary>
    public string? CellVerticalAlign { get; set; }

    /// <summary>
    /// Gets or sets the selected cell border style.
    /// </summary>
    public string? CellBorder { get; set; }

    /// <summary>
    /// Gets or sets the selected cell border style preset.
    /// </summary>
    public string? BorderStyle { get; set; }

    /// <summary>
    /// Gets or sets the selected cell border width in pixels.
    /// </summary>
    public int? BorderWidthPx { get; set; }

    /// <summary>
    /// Gets or sets the selected cell border color.
    /// </summary>
    public string? BorderColor { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the top border should be applied.
    /// </summary>
    public bool BorderTop { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the right border should be applied.
    /// </summary>
    public bool BorderRight { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the bottom border should be applied.
    /// </summary>
    public bool BorderBottom { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the left border should be applied.
    /// </summary>
    public bool BorderLeft { get; set; } = true;

    /// <summary>
    /// Gets or sets the format string used to label generated header cells. <c>{0}</c> is replaced with the 1-based column index.
    /// </summary>
    public string? DefaultColumnHeader { get; set; }
}
