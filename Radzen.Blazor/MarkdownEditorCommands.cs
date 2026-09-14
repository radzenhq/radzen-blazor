namespace Radzen;

/// <summary>
/// Contains the names of the commands supported by <c>RadzenMarkdownEditor</c>.
/// Pass them to <c>RadzenMarkdownEditor.ExecuteCommandAsync</c>.
/// </summary>
public static class MarkdownEditorCommands
{
    /// <summary>Wraps the selection in <c>**</c>.</summary>
    public const string Bold = "bold";
    /// <summary>Wraps the selection in <c>*</c>.</summary>
    public const string Italic = "italic";
    /// <summary>Wraps the selection in <c>~~</c>.</summary>
    public const string Strikethrough = "strikethrough";
    /// <summary>Makes the selected paragraphs headings of the level given as value (<c>h1</c> to <c>h6</c>) or normal text (<c>p</c>).</summary>
    public const string FormatBlock = "formatBlock";
    /// <summary>Wraps the selection in a Markdown link. The command value is the URL.</summary>
    public const string Link = "link";
    /// <summary>Wraps the selection in a Markdown image. The command value is the image URL.</summary>
    public const string Image = "image";
    /// <summary>Wraps the selection in backticks.</summary>
    public const string Code = "code";
    /// <summary>Wraps the selection in a fenced code block.</summary>
    public const string CodeBlock = "codeBlock";
    /// <summary>Prefixes the selected lines with <c>&gt; </c>.</summary>
    public const string Quote = "quote";
    /// <summary>Prefixes the selected lines with <c>- </c>.</summary>
    public const string UnorderedList = "unorderedList";
    /// <summary>Prefixes the selected lines with <c>1. </c>, <c>2. </c>, …</summary>
    public const string OrderedList = "orderedList";
    /// <summary>Prefixes the selected lines with <c>- [ ] </c>.</summary>
    public const string TaskList = "taskList";
    /// <summary>Inserts a horizontal rule (<c>---</c>) on its own line.</summary>
    public const string HorizontalRule = "horizontalRule";
    /// <summary>Replaces the selection with the command value.</summary>
    public const string InsertText = "insertText";
    /// <summary>Restores the previous state from the editor's history.</summary>
    public const string Undo = "undo";
    /// <summary>Restores the next state from the editor's history.</summary>
    public const string Redo = "redo";

    /// <summary>
    /// Inserts a table. The value is the size as <c>rows x columns</c>, for example <c>3x3</c>.
    /// </summary>
    public const string InsertTable = "insertTable";

    /// <summary>
    /// Inserts a table row above the current row.
    /// </summary>
    public const string TableRowBefore = "tableRowBefore";

    /// <summary>
    /// Inserts a table row below the current row.
    /// </summary>
    public const string TableRowAfter = "tableRowAfter";

    /// <summary>
    /// Inserts a table column before the current column.
    /// </summary>
    public const string TableColumnBefore = "tableColumnBefore";

    /// <summary>
    /// Inserts a table column after the current column.
    /// </summary>
    public const string TableColumnAfter = "tableColumnAfter";

    /// <summary>
    /// Deletes the current table row.
    /// </summary>
    public const string TableDeleteRow = "tableDeleteRow";

    /// <summary>
    /// Deletes the current table column.
    /// </summary>
    public const string TableDeleteColumn = "tableDeleteColumn";

    /// <summary>
    /// Deletes the current table.
    /// </summary>
    public const string TableDelete = "tableDelete";

    /// <summary>
    /// Sets the alignment of the current table column. The value is <c>left</c>, <c>center</c>, <c>right</c> or <c>none</c>.
    /// </summary>
    public const string TableAlign = "tableAlign";
}
