using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// A tool which inserts tables in a <see cref="RadzenHtmlEditor" />.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenHtmlEditor @bind-Value=@html&gt;
    ///  &lt;RadzenHtmlEditorTable /&gt;
    /// &lt;/RadzenHtmlEditor&gt;
    /// @code {
    ///   string html = "@lt;strong&gt;Hello&lt;/strong&gt; world!";
    /// }
    /// </code>
    /// </example>
    public partial class RadzenHtmlEditorTable : RadzenHtmlEditorButtonBase
    {
        static readonly IEnumerable<string> TextAlignOptions = new[] { "", "left", "center", "right", "justify" };
        static readonly IEnumerable<string> VerticalAlignOptions = new[] { "", "top", "middle", "bottom" };
        static readonly IEnumerable<string> BorderStyleOptions = new[] { "solid", "dashed", "dotted", "double", "none" };

        string? title;

        /// <summary>
        /// Specifies the title (tooltip) displayed when the user hovers the tool.
        /// Falls back to <see cref="RadzenHtmlEditor.TableDialogTitleText"/> when available, otherwise <c>"Insert table"</c>.
        /// </summary>
        [Parameter]
        public string Title { get => title ?? Editor?.TableDialogTitleText ?? "Insert table"; set => title = value; }

        string? rowsText;

        /// <summary>
        /// Specifies the text of the label for the number of rows.
        /// Falls back to <see cref="RadzenHtmlEditor.TableRowsText"/> when available, otherwise <c>"Rows"</c>.
        /// </summary>
        [Parameter]
        public string RowsText { get => rowsText ?? Editor?.TableRowsText ?? "Rows"; set => rowsText = value; }

        string? columnsText;

        /// <summary>
        /// Specifies the text of the label for the number of columns.
        /// Falls back to <see cref="RadzenHtmlEditor.TableColumnsText"/> when available, otherwise <c>"Columns"</c>.
        /// </summary>
        [Parameter]
        public string ColumnsText { get => columnsText ?? Editor?.TableColumnsText ?? "Columns"; set => columnsText = value; }

        string? widthText;

        /// <summary>
        /// Specifies the text of the label for the table width.
        /// Falls back to <see cref="RadzenHtmlEditor.TableWidthText"/> when available, otherwise <c>"Width"</c>.
        /// </summary>
        [Parameter]
        public string WidthText { get => widthText ?? Editor?.TableWidthText ?? "Width"; set => widthText = value; }

        string? borderText;

        /// <summary>
        /// Specifies the text of the label for the table border.
        /// Falls back to <see cref="RadzenHtmlEditor.TableBorderText"/> when available, otherwise <c>"Border"</c>.
        /// </summary>
        [Parameter]
        public string BorderText { get => borderText ?? Editor?.TableBorderText ?? "Border"; set => borderText = value; }

        string? headerRowText;

        /// <summary>
        /// Specifies the text of the header row checkbox.
        /// Falls back to <see cref="RadzenHtmlEditor.TableHeaderRowText"/> when available, otherwise <c>"Include header row"</c>.
        /// </summary>
        [Parameter]
        public string HeaderRowText { get => headerRowText ?? Editor?.TableHeaderRowText ?? "Include header row"; set => headerRowText = value; }

        string? editText;

        /// <summary>
        /// Specifies the text of the table edit section.
        /// Falls back to <see cref="RadzenHtmlEditor.TableEditText"/> when available, otherwise <c>"Edit table"</c>.
        /// </summary>
        [Parameter]
        public string EditText { get => editText ?? Editor?.TableEditText ?? "Edit table"; set => editText = value; }

        string? okText;

        /// <summary>
        /// Specifies the text of button which inserts the table.
        /// Falls back to <see cref="RadzenHtmlEditor.TableOkText"/> when available, otherwise <c>"OK"</c>.
        /// </summary>
        [Parameter]
        public string OkText { get => okText ?? Editor?.TableOkText ?? "OK"; set => okText = value; }

        string? updateText;

        /// <summary>
        /// Specifies the text of button which updates the selected table.
        /// Falls back to <see cref="RadzenHtmlEditor.TableUpdateText"/> when available, otherwise <c>"Update"</c>.
        /// </summary>
        [Parameter]
        public string UpdateText { get => updateText ?? Editor?.TableUpdateText ?? "Update"; set => updateText = value; }

        string? cancelText;

        /// <summary>
        /// Specifies the text of button which cancels table insertion and closes the dialog.
        /// Falls back to <see cref="RadzenHtmlEditor.TableCancelText"/> when available, otherwise <c>"Cancel"</c>.
        /// </summary>
        [Parameter]
        public string CancelText { get => cancelText ?? Editor?.TableCancelText ?? "Cancel"; set => cancelText = value; }

        string? insertRowAboveText;

        /// <summary>
        /// Specifies the text of the button which inserts a row above the current row.
        /// Falls back to <see cref="RadzenHtmlEditor.TableInsertRowAboveText"/> when available, otherwise <c>"Insert row above"</c>.
        /// </summary>
        [Parameter]
        public string InsertRowAboveText { get => insertRowAboveText ?? Editor?.TableInsertRowAboveText ?? "Insert row above"; set => insertRowAboveText = value; }

        string? insertRowBelowText;

        /// <summary>
        /// Specifies the text of the button which inserts a row below the current row.
        /// Falls back to <see cref="RadzenHtmlEditor.TableInsertRowBelowText"/> when available, otherwise <c>"Insert row below"</c>.
        /// </summary>
        [Parameter]
        public string InsertRowBelowText { get => insertRowBelowText ?? Editor?.TableInsertRowBelowText ?? "Insert row below"; set => insertRowBelowText = value; }

        string? insertColumnLeftText;

        /// <summary>
        /// Specifies the text of the button which inserts a column to the left.
        /// Falls back to <see cref="RadzenHtmlEditor.TableInsertColumnLeftText"/> when available, otherwise <c>"Insert column left"</c>.
        /// </summary>
        [Parameter]
        public string InsertColumnLeftText { get => insertColumnLeftText ?? Editor?.TableInsertColumnLeftText ?? "Insert column left"; set => insertColumnLeftText = value; }

        string? insertColumnRightText;

        /// <summary>
        /// Specifies the text of the button which inserts a column to the right.
        /// Falls back to <see cref="RadzenHtmlEditor.TableInsertColumnRightText"/> when available, otherwise <c>"Insert column right"</c>.
        /// </summary>
        [Parameter]
        public string InsertColumnRightText { get => insertColumnRightText ?? Editor?.TableInsertColumnRightText ?? "Insert column right"; set => insertColumnRightText = value; }

        string? deleteRowText;

        /// <summary>
        /// Specifies the text of the button which deletes the current row.
        /// Falls back to <see cref="RadzenHtmlEditor.TableDeleteRowText"/> when available, otherwise <c>"Delete row"</c>.
        /// </summary>
        [Parameter]
        public string DeleteRowText { get => deleteRowText ?? Editor?.TableDeleteRowText ?? "Delete row"; set => deleteRowText = value; }

        string? deleteColumnText;

        /// <summary>
        /// Specifies the text of the button which deletes the current column.
        /// Falls back to <see cref="RadzenHtmlEditor.TableDeleteColumnText"/> when available, otherwise <c>"Delete column"</c>.
        /// </summary>
        [Parameter]
        public string DeleteColumnText { get => deleteColumnText ?? Editor?.TableDeleteColumnText ?? "Delete column"; set => deleteColumnText = value; }

        string? deleteTableText;

        /// <summary>
        /// Specifies the text of the button which deletes the current table.
        /// Falls back to <see cref="RadzenHtmlEditor.TableDeleteText"/> when available, otherwise <c>"Delete table"</c>.
        /// </summary>
        [Parameter]
        public string DeleteTableText { get => deleteTableText ?? Editor?.TableDeleteText ?? "Delete table"; set => deleteTableText = value; }

        string? mergeRightText;

        /// <summary>
        /// Specifies the text of the button which merges the current cell with the cell to the right.
        /// Falls back to <see cref="RadzenHtmlEditor.TableMergeRightText"/> when available, otherwise <c>"Merge right"</c>.
        /// </summary>
        [Parameter]
        public string MergeRightText { get => mergeRightText ?? Editor?.TableMergeRightText ?? "Merge right"; set => mergeRightText = value; }

        string? mergeDownText;

        /// <summary>
        /// Specifies the text of the button which merges the current cell with the cell below.
        /// Falls back to <see cref="RadzenHtmlEditor.TableMergeDownText"/> when available, otherwise <c>"Merge down"</c>.
        /// </summary>
        [Parameter]
        public string MergeDownText { get => mergeDownText ?? Editor?.TableMergeDownText ?? "Merge down"; set => mergeDownText = value; }

        string? splitCellText;

        /// <summary>
        /// Specifies the text of the button which splits the current merged cell.
        /// Falls back to <see cref="RadzenHtmlEditor.TableSplitCellText"/> when available, otherwise <c>"Split cell"</c>.
        /// </summary>
        [Parameter]
        public string SplitCellText { get => splitCellText ?? Editor?.TableSplitCellText ?? "Split cell"; set => splitCellText = value; }

        string? columnWidthText;

        /// <summary>
        /// Specifies the text of the label for the selected column width.
        /// Falls back to <see cref="RadzenHtmlEditor.TableColumnWidthText"/> when available, otherwise <c>"Column width"</c>.
        /// </summary>
        [Parameter]
        public string ColumnWidthText { get => columnWidthText ?? Editor?.TableColumnWidthText ?? "Column width"; set => columnWidthText = value; }

        string? cellBackgroundText;

        /// <summary>
        /// Specifies the text of the label for the selected cell background.
        /// Falls back to <see cref="RadzenHtmlEditor.TableCellBackgroundText"/> when available, otherwise <c>"Cell background"</c>.
        /// </summary>
        [Parameter]
        public string CellBackgroundText { get => cellBackgroundText ?? Editor?.TableCellBackgroundText ?? "Cell background"; set => cellBackgroundText = value; }

        string? cellPaddingText;

        /// <summary>
        /// Specifies the text of the label for the selected cell padding.
        /// Falls back to <see cref="RadzenHtmlEditor.TableCellPaddingText"/> when available, otherwise <c>"Cell padding"</c>.
        /// </summary>
        [Parameter]
        public string CellPaddingText { get => cellPaddingText ?? Editor?.TableCellPaddingText ?? "Cell padding"; set => cellPaddingText = value; }

        string? cellTextAlignText;

        /// <summary>
        /// Specifies the text of the label for horizontal cell alignment.
        /// Falls back to <see cref="RadzenHtmlEditor.TableCellTextAlignText"/> when available, otherwise <c>"Horizontal align"</c>.
        /// </summary>
        [Parameter]
        public string CellTextAlignText { get => cellTextAlignText ?? Editor?.TableCellTextAlignText ?? "Horizontal align"; set => cellTextAlignText = value; }

        string? cellVerticalAlignText;

        /// <summary>
        /// Specifies the text of the label for vertical cell alignment.
        /// Falls back to <see cref="RadzenHtmlEditor.TableCellVerticalAlignText"/> when available, otherwise <c>"Vertical align"</c>.
        /// </summary>
        [Parameter]
        public string CellVerticalAlignText { get => cellVerticalAlignText ?? Editor?.TableCellVerticalAlignText ?? "Vertical align"; set => cellVerticalAlignText = value; }

        string? cellBorderText;

        /// <summary>
        /// Specifies the text of the label for the selected cell border.
        /// Falls back to <see cref="RadzenHtmlEditor.TableCellBorderText"/> when available, otherwise <c>"Cell border"</c>.
        /// </summary>
        [Parameter]
        public string CellBorderText { get => cellBorderText ?? Editor?.TableCellBorderText ?? "Cell border"; set => cellBorderText = value; }

        string? columnWidthPxText;

        /// <summary>
        /// Specifies the text of the label for column width in pixels.
        /// Falls back to <see cref="RadzenHtmlEditor.TableColumnWidthPxText"/> when available, otherwise <c>"Column width (px)"</c>.
        /// </summary>
        [Parameter]
        public string ColumnWidthPxText { get => columnWidthPxText ?? Editor?.TableColumnWidthPxText ?? "Column width (px)"; set => columnWidthPxText = value; }

        string? cellPaddingPxText;

        /// <summary>
        /// Specifies the text of the label for cell padding in pixels.
        /// Falls back to <see cref="RadzenHtmlEditor.TableCellPaddingPxText"/> when available, otherwise <c>"Cell padding (px)"</c>.
        /// </summary>
        [Parameter]
        public string CellPaddingPxText { get => cellPaddingPxText ?? Editor?.TableCellPaddingPxText ?? "Cell padding (px)"; set => cellPaddingPxText = value; }

        string? borderStyleText;

        /// <summary>
        /// Specifies the text of the label for the border style.
        /// Falls back to <see cref="RadzenHtmlEditor.TableBorderStyleText"/> when available, otherwise <c>"Border style"</c>.
        /// </summary>
        [Parameter]
        public string BorderStyleText { get => borderStyleText ?? Editor?.TableBorderStyleText ?? "Border style"; set => borderStyleText = value; }

        string? borderWidthPxText;

        /// <summary>
        /// Specifies the text of the label for the border width in pixels.
        /// Falls back to <see cref="RadzenHtmlEditor.TableBorderWidthPxText"/> when available, otherwise <c>"Border width (px)"</c>.
        /// </summary>
        [Parameter]
        public string BorderWidthPxText { get => borderWidthPxText ?? Editor?.TableBorderWidthPxText ?? "Border width (px)"; set => borderWidthPxText = value; }

        string? borderColorText;

        /// <summary>
        /// Specifies the text of the label for the border color.
        /// Falls back to <see cref="RadzenHtmlEditor.TableBorderColorText"/> when available, otherwise <c>"Border color"</c>.
        /// </summary>
        [Parameter]
        public string BorderColorText { get => borderColorText ?? Editor?.TableBorderColorText ?? "Border color"; set => borderColorText = value; }

        string? borderTopText;

        /// <summary>
        /// Specifies the text of the top border checkbox.
        /// Falls back to <see cref="RadzenHtmlEditor.TableBorderTopText"/> when available, otherwise <c>"Top"</c>.
        /// </summary>
        [Parameter]
        public string BorderTopText { get => borderTopText ?? Editor?.TableBorderTopText ?? "Top"; set => borderTopText = value; }

        string? borderRightText;

        /// <summary>
        /// Specifies the text of the right border checkbox.
        /// Falls back to <see cref="RadzenHtmlEditor.TableBorderRightText"/> when available, otherwise <c>"Right"</c>.
        /// </summary>
        [Parameter]
        public string BorderRightText { get => borderRightText ?? Editor?.TableBorderRightText ?? "Right"; set => borderRightText = value; }

        string? borderBottomText;

        /// <summary>
        /// Specifies the text of the bottom border checkbox.
        /// Falls back to <see cref="RadzenHtmlEditor.TableBorderBottomText"/> when available, otherwise <c>"Bottom"</c>.
        /// </summary>
        [Parameter]
        public string BorderBottomText { get => borderBottomText ?? Editor?.TableBorderBottomText ?? "Bottom"; set => borderBottomText = value; }

        string? borderLeftText;

        /// <summary>
        /// Specifies the text of the left border checkbox.
        /// Falls back to <see cref="RadzenHtmlEditor.TableBorderLeftText"/> when available, otherwise <c>"Left"</c>.
        /// </summary>
        [Parameter]
        public string BorderLeftText { get => borderLeftText ?? Editor?.TableBorderLeftText ?? "Left"; set => borderLeftText = value; }

        /// <summary>
        /// Specifies the default number of rows. Set to <c>2</c> by default.
        /// </summary>
        [Parameter]
        public int DefaultRows { get; set; } = 2;

        /// <summary>
        /// Specifies the default number of columns. Set to <c>2</c> by default.
        /// </summary>
        [Parameter]
        public int DefaultColumns { get; set; } = 2;

        /// <summary>
        /// Specifies the default width of the inserted table. Set to <c>"100%"</c> by default.
        /// </summary>
        [Parameter]
        public string DefaultWidth { get; set; } = "100%";

        /// <summary>
        /// Specifies whether the inserted table should contain a header row by default. Set to <c>true</c> by default.
        /// </summary>
        [Parameter]
        public bool DefaultHeaderRow { get; set; } = true;

        /// <summary>
        /// Specifies the default table border. Set to <c>1</c> by default.
        /// </summary>
        [Parameter]
        public int DefaultBorder { get; set; } = 1;

        HtmlEditorTableSelection Selection { get; set; } = new();

        async Task OnSubmit()
        {
            DialogService.Close(true);
            await Task.CompletedTask;
        }

        async Task InsertHtml(HtmlEditorTableCommandArgs attributes)
        {
            if (Editor != null)
            {
                await Editor.RestoreSelectionAsync();
                await Editor.ExecuteTableCommandAsync("insertTable", attributes);
            }
        }

        async Task UpdateTable(HtmlEditorTableCommandArgs attributes)
        {
            if (Editor != null)
            {
                await Editor.RestoreSelectionAsync();
                await Editor.ExecuteTableCommandAsync("updateTable", attributes);
            }
        }

        async Task ExecuteTableCommand(string command)
        {
            if (Editor != null)
            {
                await Editor.RestoreSelectionAsync();
                await Editor.ExecuteTableCommandAsync(command);
                Selection = await Editor.GetTableSelectionAsync();
            }
        }
    }
}
