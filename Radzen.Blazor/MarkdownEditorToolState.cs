namespace Radzen;

/// <summary>
/// The active formats and history availability at the current design-surface selection, reported by JavaScript.
/// </summary>
public class MarkdownEditorToolState
{
    /// <summary>The commands (see <see cref="MarkdownEditorCommands" />) active at the current selection.</summary>
    public string[]? Formats { get; set; }

    /// <summary>The block at the current selection: <c>p</c> or <c>h1</c> to <c>h6</c>; <c>null</c> when it is neither a paragraph nor a heading.</summary>
    public string? Block { get; set; }

    /// <summary>Whether the editor's history has a state to undo to.</summary>
    public bool CanUndo { get; set; }

    /// <summary>Whether the editor's history has a state to redo to.</summary>
    public bool CanRedo { get; set; }
}
