namespace Radzen;

/// <summary>
/// Contains the commands which <see cref="Radzen.Blazor.RadzenHtmlEditor" /> supports.
/// </summary>
public static class HtmlEditorCommands
{
    /// <summary>
    /// Inserts html at cursor location.
    /// </summary>
    public static string InsertHtml = "insertHtml";

    /// <summary>
    /// Centers the selected text.
    /// </summary>
    public static string AlignCenter = "justifyCenter";

    /// <summary>
    /// Aligns the selected text to the left.
    /// </summary>
    public static string AlignLeft = "justifyLeft";

    /// <summary>
    /// Aligns the selected text to the right.
    /// </summary>
    public static string AlignRight = "justifyRight";

    /// <summary>
    /// Sets the background color of the selected text.
    /// </summary>
    public static string Background = "backColor";

    /// <summary>
    /// Bolds the selected text.
    /// </summary>
    public static string Bold = "bold";

    /// <summary>
    /// Sets the text color of the selection.
    /// </summary>
    public static string Color = "foreColor";

    /// <summary>
    /// Sets the font of the selected text.
    /// </summary>
    public static string FontName = "fontName";

    /// <summary>
    /// Sets the font size of the selected text.
    /// </summary>
    public static string FontSize = "fontSize";

    /// <summary>
    /// Formats the selection as paragraph, heading etc.
    /// </summary>
    public static string FormatBlock = "formatBlock";

    /// <summary>
    /// Indents the selection.
    /// </summary>
    public static string Indent = "indent";

    /// <summary>
    /// Makes the selected text italic.
    /// </summary>
    public static string Italic = "italic";

    /// <summary>
    /// Justifies the selected text.
    /// </summary>
    public static string Justify = "justifyFull";

    /// <summary>
    /// Inserts an empty ordered list or makes an ordered list from the selected text.
    /// </summary>
    public static string OrderedList = "insertOrderedList";

    /// <summary>
    /// Outdents the selected text.
    /// </summary>
    public static string Outdent = "outdent";

    /// <summary>
    /// Repeats the last edit operations.
    /// </summary>
    public static string Redo = "redo";

    /// <summary>
    /// Removes visual formatting from the selected text.
    /// </summary>
    public static string RemoveFormat = "removeFormat";

    /// <summary>
    /// Strikes through the selected text.
    /// </summary>
    public static string StrikeThrough = "strikeThrough";

    /// <summary>
    /// Applies subscript styling to the selected text.
    /// </summary>
    public static string Subscript = "subscript";

    /// <summary>
    /// Applies superscript styling to the selected text.
    /// </summary>
    public static string Superscript = "superscript";

    /// <summary>
    /// Underlines the selected text.
    /// </summary>
    public static string Underline = "underline";

    /// <summary>
    /// Undoes the last edit operation.
    /// </summary>
    public static string Undo = "undo";

    /// <summary>
    /// Unlinks a link.
    /// </summary>
    public static string Unlink = "unlink";

    /// <summary>
    /// Inserts an empty unordered list or makes an unordered list from the selected text.
    /// </summary>
    public static string UnorderedList = "insertUnorderedList";
}

