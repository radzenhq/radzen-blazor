using System;
using System.Linq;
using System.Text.RegularExpressions;

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
            case MarkdownEditorCommands.Bold:
                return Wrap(text, start, end, "**");
            case MarkdownEditorCommands.Italic:
                return Wrap(text, start, end, "_");
            case MarkdownEditorCommands.Strikethrough:
                return Wrap(text, start, end, "~~");
            case MarkdownEditorCommands.Code:
                return Wrap(text, start, end, "`");
            case MarkdownEditorCommands.Heading:
                return Heading(text, start, end);
            case MarkdownEditorCommands.Quote:
                return PrefixLines(text, start, end, "> ");
            case MarkdownEditorCommands.UnorderedList:
                return PrefixLines(text, start, end, "- ");
            case MarkdownEditorCommands.TaskList:
                return PrefixLines(text, start, end, "- [ ] ");
            case MarkdownEditorCommands.OrderedList:
                return OrderedList(text, start, end);
            case MarkdownEditorCommands.CodeBlock:
                return CodeBlock(text, start, end);
            case MarkdownEditorCommands.HorizontalRule:
                return HorizontalRule(text, start, end);
            case MarkdownEditorCommands.Link:
                return Link(text, start, end, value, label, "[");
            case MarkdownEditorCommands.Image:
                return Link(text, start, end, value, label, "![");
            default:
                return null;
        }
    }

    static MarkdownEdit Wrap(string text, int start, int end, string token)
    {
        var selected = text.Substring(start, end - start);
        var t = token.Length;

        // Selection contains the tokens: **hi** → hi
        if (selected.Length >= 2 * t && selected.StartsWith(token, StringComparison.Ordinal) && selected.EndsWith(token, StringComparison.Ordinal))
        {
            var inner = selected.Substring(t, selected.Length - 2 * t);
            return new MarkdownEdit(start, end, inner, start, start + inner.Length);
        }

        // Tokens immediately outside the selection: **[hi]** → hi
        if (start >= t && end + t <= text.Length
            && string.CompareOrdinal(text, start - t, token, 0, t) == 0
            && string.CompareOrdinal(text, end, token, 0, t) == 0)
        {
            return new MarkdownEdit(start - t, end + t, selected, start - t, start - t + selected.Length);
        }

        return new MarkdownEdit(start, end, token + selected + token, start + t, start + t + selected.Length);
    }

    static readonly Regex OrderedPrefix = new(@"^\d+\. ", RegexOptions.Compiled);
    static readonly Regex HeadingPrefix = new(@"^(#{1,6}) ", RegexOptions.Compiled);

    /// <summary>Expands [start, end) to whole lines. A selection ending right after a newline does not include the next line.</summary>
    static (int LineStart, int LineEnd) ExpandToLines(string text, int start, int end)
    {
        var lineStart = start == 0 ? 0 : text.LastIndexOf('\n', start - 1) + 1;

        var searchFrom = end > start && text[end - 1] == '\n' ? end - 1 : end;
        var lineEnd = searchFrom < text.Length ? text.IndexOf('\n', searchFrom) : -1;
        if (lineEnd == -1)
        {
            lineEnd = text.Length;
        }

        return (lineStart, lineEnd);
    }

    static MarkdownEdit ReplaceLines(string text, int start, int end, Func<string[], string[]> transform)
    {
        var (lineStart, lineEnd) = ExpandToLines(text, start, end);
        var lines = text.Substring(lineStart, lineEnd - lineStart).Split('\n');
        var replacement = string.Join("\n", transform(lines));
        return new MarkdownEdit(lineStart, lineEnd, replacement, lineStart, lineStart + replacement.Length);
    }

    static MarkdownEdit PrefixLines(string text, int start, int end, string prefix)
    {
        return ReplaceLines(text, start, end, lines =>
            lines.All(l => l.StartsWith(prefix, StringComparison.Ordinal))
                ? lines.Select(l => l.Substring(prefix.Length)).ToArray()
                : lines.Select(l => l.StartsWith(prefix, StringComparison.Ordinal) ? l : prefix + l).ToArray());
    }

    static MarkdownEdit OrderedList(string text, int start, int end)
    {
        return ReplaceLines(text, start, end, lines =>
            lines.All(l => OrderedPrefix.IsMatch(l))
                ? lines.Select(l => OrderedPrefix.Replace(l, string.Empty, 1)).ToArray()
                : lines.Select((l, i) => $"{i + 1}. {OrderedPrefix.Replace(l, string.Empty, 1)}").ToArray());
    }

    static MarkdownEdit Heading(string text, int start, int end)
    {
        return ReplaceLines(text, start, end, lines =>
        {
            var match = HeadingPrefix.Match(lines[0]);
            var level = match.Success ? match.Groups[1].Value.Length : 0;
            var next = level >= 3 ? 0 : level + 1;
            var prefix = next == 0 ? string.Empty : new string('#', next) + " ";
            return lines.Select(l => prefix + HeadingPrefix.Replace(l, string.Empty, 1)).ToArray();
        });
    }

    static bool AtLineStart(string text, int index) => index == 0 || text[index - 1] == '\n';
    static bool AtLineEnd(string text, int index) => index == text.Length || text[index] == '\n';

    static MarkdownEdit CodeBlock(string text, int start, int end)
    {
        var selected = text.Substring(start, end - start);
        var before = AtLineStart(text, start) ? string.Empty : "\n";
        var after = AtLineEnd(text, end) ? string.Empty : "\n";
        var innerNewline = selected.EndsWith('\n') ? string.Empty : "\n";

        var replacement = before + "```\n" + selected + innerNewline + "```" + after;
        var innerStart = start + before.Length + 4;
        return new MarkdownEdit(start, end, replacement, innerStart, innerStart + selected.Length);
    }

    static MarkdownEdit HorizontalRule(string text, int start, int end)
    {
        var before = AtLineStart(text, start) ? string.Empty : "\n";
        var after = AtLineEnd(text, end) && end < text.Length ? string.Empty : "\n";
        var replacement = before + "---" + after;
        return new MarkdownEdit(start, end, replacement, start + replacement.Length, start + replacement.Length);
    }

    static MarkdownEdit Link(string text, int start, int end, string? url, string? label, string open)
    {
        var selected = text.Substring(start, end - start);
        var linkText = selected.Length > 0 ? selected : label ?? string.Empty;
        var replacement = open + linkText + "](" + (url ?? string.Empty) + ")";

        if (linkText.Length == 0)
        {
            return new MarkdownEdit(start, end, replacement, start + open.Length, start + open.Length);
        }

        return new MarkdownEdit(start, end, replacement, start + replacement.Length, start + replacement.Length);
    }
}
