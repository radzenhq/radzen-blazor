namespace Radzen.Blazor;

/// <summary>
/// Provides localizable strings used by the table tools of <see cref="RadzenHtmlEditor" />.
/// </summary>
public class HtmlEditorTableStrings
{
    /// <summary>Gets or sets the title used by the table dialog.</summary>
    public string DialogTitle { get; set; } = "Insert table";
    /// <summary>
    /// Gets or sets the format string used to label generated header cells. <c>{0}</c> is replaced with the 1-based column index.
    /// </summary>
    public string DefaultColumnHeader { get; set; } = "Column {0}";
    /// <summary>Gets or sets the rows label used by the table dialog.</summary>
    public string Rows { get; set; } = "Rows";
    /// <summary>Gets or sets the columns label used by the table dialog.</summary>
    public string Columns { get; set; } = "Columns";
    /// <summary>Gets or sets the width label used by the table dialog.</summary>
    public string Width { get; set; } = "Width";
    /// <summary>Gets or sets the border label used by the table dialog.</summary>
    public string Border { get; set; } = "Border";
    /// <summary>Gets or sets the header row toggle text used by the table dialog.</summary>
    public string HeaderRow { get; set; } = "Include header row";
    /// <summary>Gets or sets the edit section label used by the table dialog.</summary>
    public string Edit { get; set; } = "Edit table";
    /// <summary>Gets or sets the insert confirmation button text used by the table dialog.</summary>
    public string OK { get; set; } = "OK";
    /// <summary>Gets or sets the update confirmation button text used by the table dialog.</summary>
    public string Update { get; set; } = "Update";
    /// <summary>Gets or sets the cancel button text used by the table dialog.</summary>
    public string Cancel { get; set; } = "Cancel";
    /// <summary>Gets or sets the copy cells command text.</summary>
    public string CopyCells { get; set; } = "Copy cells";
    /// <summary>Gets or sets the paste cells command text.</summary>
    public string PasteCells { get; set; } = "Paste cells";
    /// <summary>Gets or sets the insert row above command text.</summary>
    public string InsertRowAbove { get; set; } = "Insert row above";
    /// <summary>Gets or sets the insert row below command text.</summary>
    public string InsertRowBelow { get; set; } = "Insert row below";
    /// <summary>Gets or sets the insert column left command text.</summary>
    public string InsertColumnLeft { get; set; } = "Insert column left";
    /// <summary>Gets or sets the insert column right command text.</summary>
    public string InsertColumnRight { get; set; } = "Insert column right";
    /// <summary>Gets or sets the delete row command text.</summary>
    public string DeleteRow { get; set; } = "Delete row";
    /// <summary>Gets or sets the delete column command text.</summary>
    public string DeleteColumn { get; set; } = "Delete column";
    /// <summary>Gets or sets the merge right command text.</summary>
    public string MergeRight { get; set; } = "Merge right";
    /// <summary>Gets or sets the merge down command text.</summary>
    public string MergeDown { get; set; } = "Merge down";
    /// <summary>Gets or sets the split cell command text.</summary>
    public string SplitCell { get; set; } = "Split cell";
    /// <summary>Gets or sets the delete table command text.</summary>
    public string DeleteTable { get; set; } = "Delete table";
    /// <summary>Gets or sets the column width label used by the table dialog.</summary>
    public string ColumnWidth { get; set; } = "Column width";
    /// <summary>Gets or sets the cell background label used by the table dialog.</summary>
    public string CellBackground { get; set; } = "Cell background";
    /// <summary>Gets or sets the cell padding label used by the table dialog.</summary>
    public string CellPadding { get; set; } = "Cell padding";
    /// <summary>Gets or sets the horizontal cell alignment label used by the table dialog.</summary>
    public string CellTextAlign { get; set; } = "Horizontal align";
    /// <summary>Gets or sets the vertical cell alignment label used by the table dialog.</summary>
    public string CellVerticalAlign { get; set; } = "Vertical align";
    /// <summary>Gets or sets the cell border label used by the table dialog.</summary>
    public string CellBorder { get; set; } = "Cell border";
    /// <summary>Gets or sets the column width in pixels label used by the table dialog.</summary>
    public string ColumnWidthPx { get; set; } = "Column width (px)";
    /// <summary>Gets or sets the cell padding in pixels label used by the table dialog.</summary>
    public string CellPaddingPx { get; set; } = "Cell padding (px)";
    /// <summary>Gets or sets the border style label used by the table dialog.</summary>
    public string BorderStyle { get; set; } = "Border style";
    /// <summary>Gets or sets the border width in pixels label used by the table dialog.</summary>
    public string BorderWidthPx { get; set; } = "Border width (px)";
    /// <summary>Gets or sets the border color label used by the table dialog.</summary>
    public string BorderColor { get; set; } = "Border color";
    /// <summary>Gets or sets the top border toggle text used by the table dialog.</summary>
    public string BorderTop { get; set; } = "Top";
    /// <summary>Gets or sets the right border toggle text used by the table dialog.</summary>
    public string BorderRight { get; set; } = "Right";
    /// <summary>Gets or sets the bottom border toggle text used by the table dialog.</summary>
    public string BorderBottom { get; set; } = "Bottom";
    /// <summary>Gets or sets the left border toggle text used by the table dialog.</summary>
    public string BorderLeft { get; set; } = "Left";
    /// <summary>Gets or sets the notification summary shown when a table action is blocked.</summary>
    public string ActionBlocked { get; set; } = "Table action blocked";
    /// <summary>Gets or sets the message shown when a table command requires the caret to be inside a table.</summary>
    public string ActionRequiresTable { get; set; } = "Place the caret inside a table to use table actions.";
    /// <summary>Gets or sets the message shown when the selected cells do not form a rectangular copy range.</summary>
    public string CopyInvalidSelection { get; set; } = "Select a rectangular range of table cells before copying.";
    /// <summary>Gets or sets the message shown when pasted cells would overlap merged cells or an irregular selection.</summary>
    public string PasteBlocked { get; set; } = "The copied cells cannot be pasted over merged cells or an irregular selection.";
    /// <summary>Gets or sets the message shown when the selected cells cannot be merged because they are not rectangular.</summary>
    public string MergeInvalidSelection { get; set; } = "The selected cells must form a valid rectangular range before they can be merged.";
}
