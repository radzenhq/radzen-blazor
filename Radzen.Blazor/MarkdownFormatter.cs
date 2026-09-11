using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    /// GitHub-style: trims whitespace off the edges, expands a collapsed caret to the surrounding word and
    /// treats every selected line separately. When every line is already inside a matching token the token
    /// is removed from the selected words (splitting the token when it contains more), otherwise any matching
    /// tokens inside the selection are stripped and each line is wrapped once.
    /// </summary>
    static MarkdownEdit ToggleInline(string text, int start, int end, string emit, Func<InlineSpan, bool> matches)
    {
        var segments = Segments(text, start, end);

        if (segments.Count == 0)
        {
            while (start < end && char.IsWhiteSpace(text[start]))
            {
                start++;
            }

            var (lineStart, lineEnd) = ExpandToLines(text, start, start);
            var (wordStart, wordEnd) = ExpandToWord(text, start, start, lineStart, lineEnd);

            if (wordStart == wordEnd)
            {
                return new MarkdownEdit(start, start, emit + emit, start + emit.Length, start + emit.Length);
            }

            segments.Add((wordStart, wordEnd));
        }

        var containing = segments.Select(segment => ContainingSpan(text, segment, matches)).ToList();

        var edits = containing.All(span => span != null)
            ? segments.Select((segment, i) => Unwrap(text, segment, containing[i]!.Value, emit)).ToList()
            : segments.Select(segment => Wrap(text, segment, emit, matches)).ToList();

        return Combine(text, edits);
    }

    /// <summary>Splits [start, end) into one whitespace-trimmed segment per non-blank line.</summary>
    static List<(int Start, int End)> Segments(string text, int start, int end)
    {
        List<(int Start, int End)> segments = [];

        for (var lineStart = start; lineStart < end;)
        {
            var newline = text.IndexOf('\n', lineStart, end - lineStart);
            var lineEnd = newline == -1 ? end : newline;
            var (segmentStart, segmentEnd) = (lineStart, lineEnd);

            while (segmentStart < segmentEnd && char.IsWhiteSpace(text[segmentStart]))
            {
                segmentStart++;
            }
            while (segmentEnd > segmentStart && char.IsWhiteSpace(text[segmentEnd - 1]))
            {
                segmentEnd--;
            }

            if (segmentStart < segmentEnd)
            {
                segments.Add((segmentStart, segmentEnd));
            }

            lineStart = lineEnd + 1;
        }

        return segments;
    }

    static (int Start, int End) ExpandToWord(string text, int start, int end, int min, int max)
    {
        while (start > min && !char.IsWhiteSpace(text[start - 1]))
        {
            start--;
        }
        while (end < max && !char.IsWhiteSpace(text[end]))
        {
            end++;
        }

        return (start, end);
    }

    readonly record struct OuterSpan(int Start, int End, int DelimiterLength);

    /// <summary>
    /// Returns the matching inline spans of the line containing <paramref name="index" /> in document coordinates.
    /// <see cref="InlineParser.ScanSpans" /> trims its input, so leading whitespace on the line shifts the offsets.
    /// </summary>
    static IEnumerable<OuterSpan> LineSpans(string text, int index, Func<InlineSpan, bool> matches)
    {
        var (lineStart, lineEnd) = ExpandToLines(text, index, index);
        var line = text.Substring(lineStart, lineEnd - lineStart);
        var offset = lineStart + line.Length - line.TrimStart().Length;

        return InlineParser.ScanSpans(line).Where(matches).Select(s => new OuterSpan(offset + s.Start, offset + s.End, s.DelimiterLength));
    }

    static OuterSpan? ContainingSpan(string text, (int Start, int End) segment, Func<InlineSpan, bool> matches)
    {
        foreach (var span in LineSpans(text, segment.Start, matches))
        {
            if (span.Start <= segment.Start && segment.End <= span.End)
            {
                return span;
            }
        }

        return null;
    }

    /// <summary>Removes the token from the words of <paramref name="segment" />, keeping the rest of the token wrapped.</summary>
    static MarkdownEdit Unwrap(string text, (int Start, int End) segment, OuterSpan span, string emit)
    {
        var innerStart = span.Start + span.DelimiterLength;
        var innerEnd = span.End - span.DelimiterLength;
        var (start, end) = ExpandToWord(text, Math.Max(segment.Start, innerStart), Math.Min(segment.End, innerEnd), innerStart, innerEnd);

        var head = Rewrap(text[innerStart..start], emit);
        var middle = text[start..end];
        var tail = Rewrap(text[end..innerEnd], emit);

        return new MarkdownEdit(span.Start, span.End, head + middle + tail, span.Start + head.Length, span.Start + head.Length + middle.Length);
    }

    /// <summary>Wraps the non-blank part of <paramref name="part" /> in <paramref name="emit" />, keeping edge whitespace outside.</summary>
    static string Rewrap(string part, string emit)
    {
        var trimmed = part.Trim();

        if (trimmed.Length == 0)
        {
            return part;
        }

        var leading = part.Length - part.TrimStart().Length;

        return part[..leading] + emit + trimmed + emit + part[(leading + trimmed.Length)..];
    }

    /// <summary>Strips matching tokens fully inside <paramref name="segment" />, then wraps it once.</summary>
    static MarkdownEdit Wrap(string text, (int Start, int End) segment, string emit, Func<InlineSpan, bool> matches)
    {
        var (start, end) = segment;
        var content = text[start..end];

        foreach (var span in LineSpans(text, start, matches).Where(s => s.Start >= start && s.End <= end).OrderByDescending(s => s.Start))
        {
            int relativeStart = span.Start - start, relativeEnd = span.End - start;
            content = content[..relativeStart]
                + content[(relativeStart + span.DelimiterLength)..(relativeEnd - span.DelimiterLength)]
                + content[relativeEnd..];
        }

        return new MarkdownEdit(start, end, emit + content + emit, start + emit.Length, start + emit.Length + content.Length);
    }

    /// <summary>Merges non-overlapping, ordered per-line edits into a single edit spanning all of them.</summary>
    static MarkdownEdit Combine(string text, List<MarkdownEdit> edits)
    {
        if (edits.Count == 1)
        {
            return edits[0];
        }

        var replacement = new StringBuilder();
        var position = edits[0].Start;

        foreach (var edit in edits)
        {
            replacement.Append(text, position, edit.Start - position).Append(edit.Replacement);
            position = edit.End;
        }

        var last = edits[^1];
        var shift = edits.Take(edits.Count - 1).Sum(edit => edit.Replacement.Length - (edit.End - edit.Start));

        return new MarkdownEdit(edits[0].Start, last.End, replacement.ToString(), edits[0].SelectionStart, last.SelectionEnd + shift);
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

    static readonly Regex FenceLine = new(@"^\s*(```|~~~)", RegexOptions.Compiled);
    static readonly Regex NonParagraphLine = new(@"^\s*([-*+] |\d+\. |> |\||---\s*$|\*\*\*\s*$|___\s*$)", RegexOptions.Compiled);

    /// <summary>Cycles the heading level of every selected paragraph or heading line; other lines are left alone.</summary>
    static MarkdownEdit Heading(string text, int start, int end)
    {
        return ReplaceLines(text, start, end, lines =>
        {
            var inFence = false;

            return lines.Select(line =>
            {
                if (FenceLine.IsMatch(line))
                {
                    inFence = !inFence;
                    return line;
                }

                if (inFence || line.Trim().Length == 0 || NonParagraphLine.IsMatch(line))
                {
                    return line;
                }

                var match = HeadingPrefix.Match(line);
                var level = match.Success ? match.Groups[1].Value.Length : 0;
                var next = level >= 3 ? 0 : level + 1;
                var prefix = next == 0 ? string.Empty : new string('#', next) + " ";
                return prefix + HeadingPrefix.Replace(line, string.Empty, 1);
            }).ToArray();
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
