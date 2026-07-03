namespace Radzen.Blazor;

/// <summary>
/// Provides localizable strings used by the table tools of <see cref="RadzenHtmlEditor" />.
/// </summary>
public class HtmlEditorTableStrings
{
    /// <summary>Gets or sets the title used by the table dialog.</summary>
    public string? DialogTitle { get; set; }
    /// <summary>
    /// Gets or sets the format string used to label generated header cells. <c>{0}</c> is replaced with the 1-based column index.
    /// </summary>
    public string? DefaultColumnHeader { get; set; }
    /// <summary>Gets or sets the rows label used by the table dialog.</summary>
    public string? Rows { get; set; }
    /// <summary>Gets or sets the columns label used by the table dialog.</summary>
    public string? Columns { get; set; }
    /// <summary>Gets or sets the width label used by the table dialog.</summary>
    public string? Width { get; set; }
    /// <summary>Gets or sets the border label used by the table dialog.</summary>
    public string? Border { get; set; }
    /// <summary>Gets or sets the header row toggle text used by the table dialog.</summary>
    public string? HeaderRow { get; set; }
    /// <summary>Gets or sets the edit section label used by the table dialog.</summary>
    public string? Edit { get; set; }
    /// <summary>Gets or sets the insert confirmation button text used by the table dialog.</summary>
    public string? OK { get; set; }
    /// <summary>Gets or sets the update confirmation button text used by the table dialog.</summary>
    public string? Update { get; set; }
    /// <summary>Gets or sets the cancel button text used by the table dialog.</summary>
    public string? Cancel { get; set; }
    /// <summary>Gets or sets the copy cells command text.</summary>
    public string? CopyCells { get; set; }
    /// <summary>Gets or sets the paste cells command text.</summary>
    public string? PasteCells { get; set; }
    /// <summary>Gets or sets the insert row above command text.</summary>
    public string? InsertRowAbove { get; set; }
    /// <summary>Gets or sets the insert row below command text.</summary>
    public string? InsertRowBelow { get; set; }
    /// <summary>Gets or sets the insert column left command text.</summary>
    public string? InsertColumnLeft { get; set; }
    /// <summary>Gets or sets the insert column right command text.</summary>
    public string? InsertColumnRight { get; set; }
    /// <summary>Gets or sets the delete row command text.</summary>
    public string? DeleteRow { get; set; }
    /// <summary>Gets or sets the delete column command text.</summary>
    public string? DeleteColumn { get; set; }
    /// <summary>Gets or sets the merge right command text.</summary>
    public string? MergeRight { get; set; }
    /// <summary>Gets or sets the merge down command text.</summary>
    public string? MergeDown { get; set; }
    /// <summary>Gets or sets the split cell command text.</summary>
    public string? SplitCell { get; set; }
    /// <summary>Gets or sets the delete table command text.</summary>
    public string? DeleteTable { get; set; }
    /// <summary>Gets or sets the column width label used by the table dialog.</summary>
    public string? ColumnWidth { get; set; }
    /// <summary>Gets or sets the cell background label used by the table dialog.</summary>
    public string? CellBackground { get; set; }
    /// <summary>Gets or sets the cell padding label used by the table dialog.</summary>
    public string? CellPadding { get; set; }
    /// <summary>Gets or sets the horizontal cell alignment label used by the table dialog.</summary>
    public string? CellTextAlign { get; set; }
    /// <summary>Gets or sets the vertical cell alignment label used by the table dialog.</summary>
    public string? CellVerticalAlign { get; set; }
    /// <summary>Gets or sets the cell border label used by the table dialog.</summary>
    public string? CellBorder { get; set; }
    /// <summary>Gets or sets the column width in pixels label used by the table dialog.</summary>
    public string? ColumnWidthPx { get; set; }
    /// <summary>Gets or sets the cell padding in pixels label used by the table dialog.</summary>
    public string? CellPaddingPx { get; set; }
    /// <summary>Gets or sets the border style label used by the table dialog.</summary>
    public string? BorderStyle { get; set; }
    /// <summary>Gets or sets the border width in pixels label used by the table dialog.</summary>
    public string? BorderWidthPx { get; set; }
    /// <summary>Gets or sets the border color label used by the table dialog.</summary>
    public string? BorderColor { get; set; }
    /// <summary>Gets or sets the top border toggle text used by the table dialog.</summary>
    public string? BorderTop { get; set; }
    /// <summary>Gets or sets the right border toggle text used by the table dialog.</summary>
    public string? BorderRight { get; set; }
    /// <summary>Gets or sets the bottom border toggle text used by the table dialog.</summary>
    public string? BorderBottom { get; set; }
    /// <summary>Gets or sets the left border toggle text used by the table dialog.</summary>
    public string? BorderLeft { get; set; }
    /// <summary>Gets or sets the notification summary shown when a table action is blocked.</summary>
    public string? ActionBlocked { get; set; }
    /// <summary>Gets or sets the message shown when a table command requires the caret to be inside a table.</summary>
    public string? ActionRequiresTable { get; set; }
    /// <summary>Gets or sets the message shown when the selected cells do not form a rectangular copy range.</summary>
    public string? CopyInvalidSelection { get; set; }
    /// <summary>Gets or sets the message shown when pasted cells would overlap merged cells or an irregular selection.</summary>
    public string? PasteBlocked { get; set; }
    /// <summary>Gets or sets the message shown when the selected cells cannot be merged because they are not rectangular.</summary>
    public string? MergeInvalidSelection { get; set; }
}
