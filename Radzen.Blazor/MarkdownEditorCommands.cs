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
    /// <summary>Cycles the heading level of the selected lines (# → ## → ### → none).</summary>
    public const string Heading = "heading";
    /// <summary>Wraps the selection in a markdown link. The command value is the URL.</summary>
    public const string Link = "link";
    /// <summary>Wraps the selection in a markdown image. The command value is the image URL.</summary>
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
}
