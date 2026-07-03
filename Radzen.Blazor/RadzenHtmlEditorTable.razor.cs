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
        /// Falls back to <see cref="RadzenHtmlEditor.TableStrings"/> when available, otherwise a localized default.
        /// </summary>
        [Parameter]
        public string Title { get => title ?? Editor?.TableStrings.DialogTitle ?? Localize(nameof(RadzenStrings.HtmlEditorTable_DialogTitle)); set => title = value; }

        string? rowsText;

        /// <summary>
        /// Specifies the text of the label for the number of rows.
        /// Falls back to <see cref="RadzenHtmlEditor.TableStrings"/> when available, otherwise a localized default.
        /// </summary>
        [Parameter]
        public string RowsText { get => rowsText ?? Editor?.TableStrings.Rows ?? Localize(nameof(RadzenStrings.HtmlEditorTable_Rows)); set => rowsText = value; }

        string? columnsText;

        /// <summary>
        /// Specifies the text of the label for the number of columns.
        /// Falls back to <see cref="RadzenHtmlEditor.TableStrings"/> when available, otherwise a localized default.
        /// </summary>
        [Parameter]
        public string ColumnsText { get => columnsText ?? Editor?.TableStrings.Columns ?? Localize(nameof(RadzenStrings.HtmlEditorTable_Columns)); set => columnsText = value; }

        string? widthText;

        /// <summary>
        /// Specifies the text of the label for the table width.
        /// Falls back to <see cref="RadzenHtmlEditor.TableStrings"/> when available, otherwise a localized default.
        /// </summary>
        [Parameter]
        public string WidthText { get => widthText ?? Editor?.TableStrings.Width ?? Localize(nameof(RadzenStrings.HtmlEditorTable_Width)); set => widthText = value; }

        string? borderText;

        /// <summary>
        /// Specifies the text of the label for the table border.
        /// Falls back to <see cref="RadzenHtmlEditor.TableStrings"/> when available, otherwise a localized default.
        /// </summary>
        [Parameter]
        public string BorderText { get => borderText ?? Editor?.TableStrings.Border ?? Localize(nameof(RadzenStrings.HtmlEditorTable_Border)); set => borderText = value; }

        string? headerRowText;

        /// <summary>
        /// Specifies the text of the header row checkbox.
        /// Falls back to <see cref="RadzenHtmlEditor.TableStrings"/> when available, otherwise a localized default.
        /// </summary>
        [Parameter]
        public string HeaderRowText { get => headerRowText ?? Editor?.TableStrings.HeaderRow ?? Localize(nameof(RadzenStrings.HtmlEditorTable_HeaderRow)); set => headerRowText = value; }

        string? editText;

        /// <summary>
        /// Specifies the text of the table edit section.
        /// Falls back to <see cref="RadzenHtmlEditor.TableStrings"/> when available, otherwise a localized default.
        /// </summary>
        [Parameter]
        public string EditText { get => editText ?? Editor?.TableStrings.Edit ?? Localize(nameof(RadzenStrings.HtmlEditorTable_Edit)); set => editText = value; }

        string? okText;

        /// <summary>
        /// Specifies the text of button which inserts the table.
        /// Falls back to <see cref="RadzenHtmlEditor.TableStrings"/> when available, otherwise a localized default.
        /// </summary>
        [Parameter]
        public string OkText { get => okText ?? Editor?.TableStrings.OK ?? Localize(nameof(RadzenStrings.HtmlEditorTable_OK)); set => okText = value; }

        string? updateText;

        /// <summary>
        /// Specifies the text of button which updates the selected table.
        /// Falls back to <see cref="RadzenHtmlEditor.TableStrings"/> when available, otherwise a localized default.
        /// </summary>
        [Parameter]
        public string UpdateText { get => updateText ?? Editor?.TableStrings.Update ?? Localize(nameof(RadzenStrings.HtmlEditorTable_Update)); set => updateText = value; }

        string? cancelText;

        /// <summary>
        /// Specifies the text of button which cancels table insertion and closes the dialog.
        /// Falls back to <see cref="RadzenHtmlEditor.TableStrings"/> when available, otherwise a localized default.
        /// </summary>
        [Parameter]
        public string CancelText { get => cancelText ?? Editor?.TableStrings.Cancel ?? Localize(nameof(RadzenStrings.HtmlEditorTable_Cancel)); set => cancelText = value; }

        string? insertRowAboveText;

        /// <summary>
        /// Specifies the text of the button which inserts a row above the current row.
        /// Falls back to <see cref="RadzenHtmlEditor.TableStrings"/> when available, otherwise a localized default.
        /// </summary>
        [Parameter]
        public string InsertRowAboveText { get => insertRowAboveText ?? Editor?.TableStrings.InsertRowAbove ?? Localize(nameof(RadzenStrings.HtmlEditorTable_InsertRowAbove)); set => insertRowAboveText = value; }

        string? insertRowBelowText;

        /// <summary>
        /// Specifies the text of the button which inserts a row below the current row.
        /// Falls back to <see cref="RadzenHtmlEditor.TableStrings"/> when available, otherwise a localized default.
        /// </summary>
        [Parameter]
        public string InsertRowBelowText { get => insertRowBelowText ?? Editor?.TableStrings.InsertRowBelow ?? Localize(nameof(RadzenStrings.HtmlEditorTable_InsertRowBelow)); set => insertRowBelowText = value; }

        string? insertColumnLeftText;

        /// <summary>
        /// Specifies the text of the button which inserts a column to the left.
        /// Falls back to <see cref="RadzenHtmlEditor.TableStrings"/> when available, otherwise a localized default.
        /// </summary>
        [Parameter]
        public string InsertColumnLeftText { get => insertColumnLeftText ?? Editor?.TableStrings.InsertColumnLeft ?? Localize(nameof(RadzenStrings.HtmlEditorTable_InsertColumnLeft)); set => insertColumnLeftText = value; }

        string? insertColumnRightText;

        /// <summary>
        /// Specifies the text of the button which inserts a column to the right.
        /// Falls back to <see cref="RadzenHtmlEditor.TableStrings"/> when available, otherwise a localized default.
        /// </summary>
        [Parameter]
        public string InsertColumnRightText { get => insertColumnRightText ?? Editor?.TableStrings.InsertColumnRight ?? Localize(nameof(RadzenStrings.HtmlEditorTable_InsertColumnRight)); set => insertColumnRightText = value; }

        string? deleteRowText;

        /// <summary>
        /// Specifies the text of the button which deletes the current row.
        /// Falls back to <see cref="RadzenHtmlEditor.TableStrings"/> when available, otherwise a localized default.
        /// </summary>
        [Parameter]
        public string DeleteRowText { get => deleteRowText ?? Editor?.TableStrings.DeleteRow ?? Localize(nameof(RadzenStrings.HtmlEditorTable_DeleteRow)); set => deleteRowText = value; }

        string? deleteColumnText;

        /// <summary>
        /// Specifies the text of the button which deletes the current column.
        /// Falls back to <see cref="RadzenHtmlEditor.TableStrings"/> when available, otherwise a localized default.
        /// </summary>
        [Parameter]
        public string DeleteColumnText { get => deleteColumnText ?? Editor?.TableStrings.DeleteColumn ?? Localize(nameof(RadzenStrings.HtmlEditorTable_DeleteColumn)); set => deleteColumnText = value; }

        string? deleteTableText;

        /// <summary>
        /// Specifies the text of the button which deletes the current table.
        /// Falls back to <see cref="RadzenHtmlEditor.TableStrings"/> when available, otherwise a localized default.
        /// </summary>
        [Parameter]
        public string DeleteTableText { get => deleteTableText ?? Editor?.TableStrings.DeleteTable ?? Localize(nameof(RadzenStrings.HtmlEditorTable_DeleteTable)); set => deleteTableText = value; }

        string? mergeRightText;

        /// <summary>
        /// Specifies the text of the button which merges the current cell with the cell to the right.
        /// Falls back to <see cref="RadzenHtmlEditor.TableStrings"/> when available, otherwise a localized default.
        /// </summary>
        [Parameter]
        public string MergeRightText { get => mergeRightText ?? Editor?.TableStrings.MergeRight ?? Localize(nameof(RadzenStrings.HtmlEditorTable_MergeRight)); set => mergeRightText = value; }

        string? mergeDownText;

        /// <summary>
        /// Specifies the text of the button which merges the current cell with the cell below.
        /// Falls back to <see cref="RadzenHtmlEditor.TableStrings"/> when available, otherwise a localized default.
        /// </summary>
        [Parameter]
        public string MergeDownText { get => mergeDownText ?? Editor?.TableStrings.MergeDown ?? Localize(nameof(RadzenStrings.HtmlEditorTable_MergeDown)); set => mergeDownText = value; }

        string? splitCellText;

        /// <summary>
        /// Specifies the text of the button which splits the current merged cell.
        /// Falls back to <see cref="RadzenHtmlEditor.TableStrings"/> when available, otherwise a localized default.
        /// </summary>
        [Parameter]
        public string SplitCellText { get => splitCellText ?? Editor?.TableStrings.SplitCell ?? Localize(nameof(RadzenStrings.HtmlEditorTable_SplitCell)); set => splitCellText = value; }

        string? columnWidthText;

        /// <summary>
        /// Specifies the text of the label for the selected column width.
        /// Falls back to <see cref="RadzenHtmlEditor.TableStrings"/> when available, otherwise a localized default.
        /// </summary>
        [Parameter]
        public string ColumnWidthText { get => columnWidthText ?? Editor?.TableStrings.ColumnWidth ?? Localize(nameof(RadzenStrings.HtmlEditorTable_ColumnWidth)); set => columnWidthText = value; }

        string? cellBackgroundText;

        /// <summary>
        /// Specifies the text of the label for the selected cell background.
        /// Falls back to <see cref="RadzenHtmlEditor.TableStrings"/> when available, otherwise a localized default.
        /// </summary>
        [Parameter]
        public string CellBackgroundText { get => cellBackgroundText ?? Editor?.TableStrings.CellBackground ?? Localize(nameof(RadzenStrings.HtmlEditorTable_CellBackground)); set => cellBackgroundText = value; }

        string? cellPaddingText;

        /// <summary>
        /// Specifies the text of the label for the selected cell padding.
        /// Falls back to <see cref="RadzenHtmlEditor.TableStrings"/> when available, otherwise a localized default.
        /// </summary>
        [Parameter]
        public string CellPaddingText { get => cellPaddingText ?? Editor?.TableStrings.CellPadding ?? Localize(nameof(RadzenStrings.HtmlEditorTable_CellPadding)); set => cellPaddingText = value; }

        string? cellTextAlignText;

        /// <summary>
        /// Specifies the text of the label for horizontal cell alignment.
        /// Falls back to <see cref="RadzenHtmlEditor.TableStrings"/> when available, otherwise a localized default.
        /// </summary>
        [Parameter]
        public string CellTextAlignText { get => cellTextAlignText ?? Editor?.TableStrings.CellTextAlign ?? Localize(nameof(RadzenStrings.HtmlEditorTable_CellTextAlign)); set => cellTextAlignText = value; }

        string? cellVerticalAlignText;

        /// <summary>
        /// Specifies the text of the label for vertical cell alignment.
        /// Falls back to <see cref="RadzenHtmlEditor.TableStrings"/> when available, otherwise a localized default.
        /// </summary>
        [Parameter]
        public string CellVerticalAlignText { get => cellVerticalAlignText ?? Editor?.TableStrings.CellVerticalAlign ?? Localize(nameof(RadzenStrings.HtmlEditorTable_CellVerticalAlign)); set => cellVerticalAlignText = value; }

        string? cellBorderText;

        /// <summary>
        /// Specifies the text of the label for the selected cell border.
        /// Falls back to <see cref="RadzenHtmlEditor.TableStrings"/> when available, otherwise a localized default.
        /// </summary>
        [Parameter]
        public string CellBorderText { get => cellBorderText ?? Editor?.TableStrings.CellBorder ?? Localize(nameof(RadzenStrings.HtmlEditorTable_CellBorder)); set => cellBorderText = value; }

        string? columnWidthPxText;

        /// <summary>
        /// Specifies the text of the label for column width in pixels.
        /// Falls back to <see cref="RadzenHtmlEditor.TableStrings"/> when available, otherwise a localized default.
        /// </summary>
        [Parameter]
        public string ColumnWidthPxText { get => columnWidthPxText ?? Editor?.TableStrings.ColumnWidthPx ?? Localize(nameof(RadzenStrings.HtmlEditorTable_ColumnWidthPx)); set => columnWidthPxText = value; }

        string? cellPaddingPxText;

        /// <summary>
        /// Specifies the text of the label for cell padding in pixels.
        /// Falls back to <see cref="RadzenHtmlEditor.TableStrings"/> when available, otherwise a localized default.
        /// </summary>
        [Parameter]
        public string CellPaddingPxText { get => cellPaddingPxText ?? Editor?.TableStrings.CellPaddingPx ?? Localize(nameof(RadzenStrings.HtmlEditorTable_CellPaddingPx)); set => cellPaddingPxText = value; }

        string? borderStyleText;

        /// <summary>
        /// Specifies the text of the label for the border style.
        /// Falls back to <see cref="RadzenHtmlEditor.TableStrings"/> when available, otherwise a localized default.
        /// </summary>
        [Parameter]
        public string BorderStyleText { get => borderStyleText ?? Editor?.TableStrings.BorderStyle ?? Localize(nameof(RadzenStrings.HtmlEditorTable_BorderStyle)); set => borderStyleText = value; }

        string? borderWidthPxText;

        /// <summary>
        /// Specifies the text of the label for the border width in pixels.
        /// Falls back to <see cref="RadzenHtmlEditor.TableStrings"/> when available, otherwise a localized default.
        /// </summary>
        [Parameter]
        public string BorderWidthPxText { get => borderWidthPxText ?? Editor?.TableStrings.BorderWidthPx ?? Localize(nameof(RadzenStrings.HtmlEditorTable_BorderWidthPx)); set => borderWidthPxText = value; }

        string? borderColorText;

        /// <summary>
        /// Specifies the text of the label for the border color.
        /// Falls back to <see cref="RadzenHtmlEditor.TableStrings"/> when available, otherwise a localized default.
        /// </summary>
        [Parameter]
        public string BorderColorText { get => borderColorText ?? Editor?.TableStrings.BorderColor ?? Localize(nameof(RadzenStrings.HtmlEditorTable_BorderColor)); set => borderColorText = value; }

        string? borderTopText;

        /// <summary>
        /// Specifies the text of the top border checkbox.
        /// Falls back to <see cref="RadzenHtmlEditor.TableStrings"/> when available, otherwise a localized default.
        /// </summary>
        [Parameter]
        public string BorderTopText { get => borderTopText ?? Editor?.TableStrings.BorderTop ?? Localize(nameof(RadzenStrings.HtmlEditorTable_BorderTop)); set => borderTopText = value; }

        string? borderRightText;

        /// <summary>
        /// Specifies the text of the right border checkbox.
        /// Falls back to <see cref="RadzenHtmlEditor.TableStrings"/> when available, otherwise a localized default.
        /// </summary>
        [Parameter]
        public string BorderRightText { get => borderRightText ?? Editor?.TableStrings.BorderRight ?? Localize(nameof(RadzenStrings.HtmlEditorTable_BorderRight)); set => borderRightText = value; }

        string? borderBottomText;

        /// <summary>
        /// Specifies the text of the bottom border checkbox.
        /// Falls back to <see cref="RadzenHtmlEditor.TableStrings"/> when available, otherwise a localized default.
        /// </summary>
        [Parameter]
        public string BorderBottomText { get => borderBottomText ?? Editor?.TableStrings.BorderBottom ?? Localize(nameof(RadzenStrings.HtmlEditorTable_BorderBottom)); set => borderBottomText = value; }

        string? borderLeftText;

        /// <summary>
        /// Specifies the text of the left border checkbox.
        /// Falls back to <see cref="RadzenHtmlEditor.TableStrings"/> when available, otherwise a localized default.
        /// </summary>
        [Parameter]
        public string BorderLeftText { get => borderLeftText ?? Editor?.TableStrings.BorderLeft ?? Localize(nameof(RadzenStrings.HtmlEditorTable_BorderLeft)); set => borderLeftText = value; }

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
