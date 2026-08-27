using System;

namespace Radzen.Blazor;

/// <summary>
/// A text edit produced by <see cref="MarkdownFormatter" />: replace <c>[Start, End)</c> with <see cref="Replacement" />
/// and afterwards select <c>[SelectionStart, SelectionEnd)</c>.
/// </summary>
internal readonly record struct MarkdownEdit(int Start, int End, string Replacement, int SelectionStart, int SelectionEnd);

/// <summary>
/// Pure markdown formatting logic used by <c>RadzenMarkdownEditor</c>. Computes the minimal replacement
/// for a command so the browser can apply it as a single undoable edit.
/// </summary>
internal static class MarkdownFormatter
{
    /// <summary>
    /// Computes the edit for <paramref name="command" /> applied to the selection <c>[start, end)</c> of <paramref name="text" />.
    /// Returns <c>null</c> for unknown commands.
    /// </summary>
    /// <param name="text">The current editor text.</param>
    /// <param name="start">Selection start (clamped).</param>
    /// <param name="end">Selection end (clamped).</param>
    /// <param name="command">One of <see cref="MarkdownEditorCommands" />.</param>
    /// <param name="value">Command value: the URL for link/image, the text for insertText.</param>
    /// <param name="label">Optional link/image label used when the selection is empty.</param>
    public static MarkdownEdit? Apply(string text, int start, int end, string command, string? value = null, string? label = null)
    {
        text ??= string.Empty;
        start = Math.Clamp(start, 0, text.Length);
        end = Math.Clamp(end, start, text.Length);

        switch (command)
        {
            case MarkdownEditorCommands.InsertText:
                var inserted = value ?? string.Empty;
                return new MarkdownEdit(start, end, inserted, start + inserted.Length, start + inserted.Length);
            default:
                return null;
        }
    }
}
