using System;
using System.Linq;
using System.Text.RegularExpressions;
using Radzen.Documents.Markdown;

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
                return ToggleInline(text, start, end, "**", s => s.Char is '*' or '_' && s.DelimiterLength == 2);
            case MarkdownEditorCommands.Italic:
                return ToggleInline(text, start, end, "*", s => s.Char is '*' or '_' && s.DelimiterLength == 1);
            case MarkdownEditorCommands.Strikethrough:
                return ToggleInline(text, start, end, "~~", s => s.Char == '~');
            case MarkdownEditorCommands.Code:
                return ToggleInline(text, start, end, "`", s => s.Char == '`');
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

    /// <summary>
    /// Toggles an inline markdown token (bold/italic/strikethrough/code) on the selection, normalizing it
    /// GitHub-style: trims whitespace off the edges, expands a collapsed caret to the surrounding word,
    /// unwraps a token that already contains the selection, and otherwise strips any fully-selected matching
    /// tokens before wrapping once.
    /// </summary>
    static MarkdownEdit ToggleInline(string text, int start, int end, string emit, Func<InlineSpan, bool> matches)
    {
        // 1. scan only the lines the selection touches — inline tokens never matter across paragraphs here
        var (lineStart, lineEnd) = ExpandToLines(text, start, end);
        var line = text.Substring(lineStart, lineEnd - lineStart);
        var spans = InlineParser.ScanSpans(line);

        // ScanSpans trims its input, so span offsets are relative to the trimmed line. Leading whitespace
        // on the line shifts them; trailing whitespace does not affect start-relative offsets.
        var leadingWhitespace = line.Length - line.TrimStart().Length;

        // 2. trim whitespace off the selection edges
        while (start < end && char.IsWhiteSpace(text[start]))
        {
            start++;
        }
        while (end > start && char.IsWhiteSpace(text[end - 1]))
        {
            end--;
        }

        // 3. collapsed caret: expand to the surrounding non-whitespace run
        if (start == end)
        {
            while (start > lineStart && !char.IsWhiteSpace(text[start - 1]))
            {
                start--;
            }
            while (end < lineEnd && !char.IsWhiteSpace(text[end]))
            {
                end++;
            }
            if (start == end)
            {
                // caret in whitespace: insert empty delimiters, caret between them
                return new MarkdownEdit(start, end, emit + emit, start + emit.Length, start + emit.Length);
            }
        }

        // 4a. a matching token whose outer range contains the selection → unwrap it
        foreach (var s in spans)
        {
            if (!matches(s))
            {
                continue;
            }
            int outerStart = lineStart + leadingWhitespace + s.Start, outerEnd = lineStart + leadingWhitespace + s.End;
            if (outerStart <= start && end <= outerEnd)
            {
                var inner = text.Substring(outerStart + s.DelimiterLength, outerEnd - outerStart - 2 * s.DelimiterLength);
                return new MarkdownEdit(outerStart, outerEnd, inner, outerStart, outerStart + inner.Length);
            }
        }

        // 4b. otherwise: strip matching tokens fully inside the selection, then wrap once
        var content = text.Substring(start, end - start);
        foreach (var s in spans.Where(s => matches(s)
                     && lineStart + leadingWhitespace + s.Start >= start && lineStart + leadingWhitespace + s.End <= end)
                 .OrderByDescending(s => s.Start))
        {
            int rs = lineStart + leadingWhitespace + s.Start - start, re = lineStart + leadingWhitespace + s.End - start;
            content = content[..rs]
                + content[(rs + s.DelimiterLength)..(re - s.DelimiterLength)]
                + content[re..];
        }

        var replacement = emit + content + emit;
        return new MarkdownEdit(start, end, replacement, start + emit.Length, start + emit.Length + content.Length);
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
        var before = AtLineStart(text, end) ? string.Empty : "\n";
        var after = AtLineEnd(text, end) && end < text.Length ? string.Empty : "\n";
        var replacement = before + "---" + after;
        return new MarkdownEdit(end, end, replacement, end + replacement.Length, end + replacement.Length);
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
