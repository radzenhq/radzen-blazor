namespace Radzen;

/// <summary>
/// The active formats and history availability at the current design-surface selection, reported by JavaScript.
/// </summary>
public class MarkdownEditorToolState
{
    /// <summary>The commands (see <see cref="MarkdownEditorCommands" />) active at the current selection.</summary>
    public string[]? Formats { get; set; }

    /// <summary>
    /// The block at the current selection: <c>p</c> or <c>h1</c> to <c>h6</c>, an empty string when the selection spans several kinds of blocks,
    /// or <c>null</c> when it contains no paragraph or heading at all.
    /// </summary>
    public string? Block { get; set; }

    /// <summary>Whether the editor's history has a state to undo to.</summary>
    public bool CanUndo { get; set; }

    /// <summary>Whether the editor's history has a state to redo to.</summary>
    public bool CanRedo { get; set; }

    /// <summary>
    /// The index of the table row at the caret, or <c>-1</c> outside a table.
    /// </summary>
    public int TableRow { get; set; } = -1;

    /// <summary>
    /// The index of the table column at the caret, or <c>-1</c> outside a table.
    /// </summary>
    public int TableColumn { get; set; } = -1;

    /// <summary>
    /// The number of rows of the table at the caret, or <c>0</c> outside a table.
    /// </summary>
    public int TableRows { get; set; }

    /// <summary>
    /// The number of columns of the table at the caret, or <c>0</c> outside a table.
    /// </summary>
    public int TableColumns { get; set; }

    /// <summary>
    /// The alignment of the table column at the caret: <c>none</c>, <c>left</c>, <c>center</c> or <c>right</c>.
    /// </summary>
    public string? TableAlignment { get; set; }
}
