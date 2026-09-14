using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Radzen.Documents.Markdown;

internal sealed class MarkdownWriter : INodeVisitor
{
    private readonly string source;
    private readonly StringBuilder output = new();
    private readonly Dictionary<INode, (int Start, int End)> positions = new(ReferenceEqualityComparer.Instance);
    private string delimiter = string.Empty;
    private Block? closed;
    private bool tight;
    private bool inTable;
    private bool inHeading;

    private readonly bool verbatimBlocks;

    private MarkdownWriter(string source, string delimiter, bool verbatimBlocks)
    {
        this.source = source;
        this.delimiter = delimiter;
        this.verbatimBlocks = verbatimBlocks;
    }

    public IReadOnlyDictionary<INode, (int Start, int End)> Positions => positions;

    public string Text => output.ToString();

    public static string Write(Document document, string source, bool verbatimBlocks = false) => WriteDocument(document, source, verbatimBlocks).Text;

    public static MarkdownWriter WriteDocument(Document document, string source, bool verbatimBlocks = true)
    {
        var writer = new MarkdownWriter(source, string.Empty, verbatimBlocks);
        document.Accept(writer);
        return writer;
    }

    public static MarkdownWriter Write(IEnumerable<Block> blocks, string source, string delimiter = "")
    {
        var writer = new MarkdownWriter(source, delimiter, false);

        foreach (var block in blocks)
        {
            block.Accept(writer);
        }

        return writer;
    }

    private bool AtBlank => output.Length == 0 || output[^1] == '\n';

    private static bool IsPristine(Block block) => block.Pristine && block switch
    {
        BlockContainer container => container.Children.All(IsPristine),
        Table table => table.Rows.All(row => row.Cells.All(cell => cell.Pristine && cell.Children.All(Verbatim))),
        Leaf leaf => leaf.Children.All(Verbatim),
        _ => true
    };

    private readonly Dictionary<Text, bool> escaped = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<Block, string> prefixes = new(ReferenceEqualityComparer.Instance);

    public int OffsetWithin(Block block, int valueOffset)
    {
        var start = positions.TryGetValue(block, out var position) ? position.Start : 0;
        var prefix = prefixes.TryGetValue(block, out var stored) ? stored : string.Empty;
        var value = block is Leaf leaf ? leaf.Value : string.Empty;
        var newlines = 0;

        for (var index = 0; index < valueOffset && index < value.Length; index++)
        {
            if (value[index] == '\n')
            {
                newlines++;
            }
        }

        return start + valueOffset + newlines * prefix.Length;
    }

    public bool Knows(Text text) => positions.ContainsKey(text) || expelled.ContainsKey(text);

    public int OffsetWithin(Text text, int valueOffset)
    {
        if (expelled.TryGetValue(text, out var expel) && !positions.ContainsKey(text))
        {
            var inner = positions[expel.Replacement];

            if (valueOffset < expel.Leading)
            {
                return inner.Start - expel.Leading - MarkerLengthBefore(expel.Replacement) + valueOffset;
            }

            if (valueOffset > expel.Leading + expel.Replacement.Value.Length)
            {
                return inner.End + MarkerLengthAfter(expel.Replacement) + (valueOffset - expel.Leading - expel.Replacement.Value.Length);
            }

            return OffsetWithin(expel.Replacement, valueOffset - expel.Leading);
        }

        var start = positions[text].Start;

        if (escaped.TryGetValue(text, out var lineStart))
        {
            return start + EscapedOffset(text.Value, lineStart, Math.Min(valueOffset, text.Value.Length));
        }

        return start + SourceOffsetWithin(source, text, valueOffset);
    }

    public static int SourceOffsetWithin(string source, Text text, int valueOffset)
    {
        var slice = text.SourceEnd > text.SourceStart && text.SourceEnd <= source.Length ? source[text.SourceStart..text.SourceEnd] : text.Value;
        var i = 0;
        var j = 0;

        while (j < valueOffset && i < slice.Length)
        {
            if (slice[i] == '\\' && i + 1 < slice.Length && j < text.Value.Length && slice[i + 1] == text.Value[j])
            {
                i += 2;
            }
            else
            {
                i++;
            }

            j++;
        }

        return i;
    }

    public static int ValueOffsetWithin(string source, Text text, int sourceOffset)
    {
        var slice = text.SourceEnd > text.SourceStart && text.SourceEnd <= source.Length ? source[text.SourceStart..text.SourceEnd] : text.Value;
        var i = 0;
        var j = 0;

        while (i < sourceOffset && i < slice.Length)
        {
            if (slice[i] == '\\' && i + 1 < slice.Length && j < text.Value.Length && slice[i + 1] == text.Value[j])
            {
                i += 2;
            }
            else
            {
                i++;
            }

            j++;
        }

        return Math.Min(j, text.Value.Length);
    }

    private void FlushClose(int size = 2)
    {
        if (closed == null)
        {
            return;
        }

        if (!AtBlank)
        {
            output.Append('\n');
        }

        var trimmed = delimiter.TrimEnd();

        for (var index = 1; index < size; index++)
        {
            output.Append(trimmed).Append('\n');
        }

        closed = null;
    }

    private void Write(string text)
    {
        FlushClose(tight ? 1 : 2);

        if (delimiter.Length > 0 && AtBlank)
        {
            output.Append(delimiter);
        }

        output.Append(text);
    }

    private void EnsureNewLine()
    {
        if (!AtBlank)
        {
            output.Append('\n');
        }
    }

    private void CloseBlock(Block block) => closed = block;

    private void WrapBlock(string delim, string? firstDelim, Block block, Action content)
    {
        var previous = delimiter;
        Write(firstDelim ?? delim);
        delimiter += delim;
        content();
        delimiter = previous;
        CloseBlock(block);
    }

    private void RenderBlocks(IReadOnlyList<Block> blocks)
    {
        foreach (var block in blocks)
        {
            block.Accept(this);
        }
    }

    private int Emit(string text)
    {
        if (delimiter.Length > 0 && AtBlank)
        {
            output.Append(delimiter);
        }

        var start = output.Length;
        output.Append(text);
        return start;
    }

    private string Slice(int start, int end) => start >= 0 && end <= source.Length && end > start ? source[start..end] : string.Empty;

    private static bool Verbatim(Inline inline) => inline.Pristine && inline.SourceEnd > inline.SourceStart && (inline is not InlineContainer container || container.Children.All(Verbatim));

    public void VisitDocument(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);
        Block? previous = null;
        var pending = new List<Paragraph>();

        foreach (var block in document.Children)
        {
            if (block is Paragraph { Virtual: true, Children.Count: 0 })
            {
                continue;
            }

            if (block is Paragraph { Children.Count: 0, Pristine: true } empty && verbatimBlocks)
            {
                pending.Add(empty);
                continue;
            }

            var verbatim = verbatimBlocks && IsPristine(block) && block.SourceEnd > block.SourceStart;
            var previousVerbatim = previous != null && verbatimBlocks && IsPristine(previous) && previous.SourceEnd > previous.SourceStart && previous.SourceEnd <= block.SourceStart;

            if (verbatim && previousVerbatim && BlankLines.Placeholders(source, previous!.SourceEnd, block.SourceStart, true, true).Count == pending.Count)
            {
                closed = null;
                output.Append(source, previous.SourceEnd, block.SourceStart - previous.SourceEnd);
                pending.Clear();
            }
            else
            {
                foreach (var paragraph in pending)
                {
                    paragraph.Accept(this);
                }

                pending.Clear();
            }

            if (verbatim)
            {
                FlushClose();
                var start = output.Length;
                output.Append(source, block.SourceStart, block.SourceEnd - block.SourceStart);
                positions[block] = (start, output.Length);
                CloseBlock(block);
            }
            else
            {
                block.Accept(this);
            }

            previous = block;
        }

        foreach (var paragraph in pending)
        {
            paragraph.Accept(this);
        }

        foreach (var definition in document.LinkReferenceDefinitions)
        {
            Write(definition);
            CloseBlock(document);
        }

    }

    private static bool HasFollowing(Block block)
    {
        while (block.Parent != null)
        {
            var next = block.Parent.NextSibling(block);

            while (next is Paragraph { Virtual: true, Children.Count: 0 })
            {
                next = block.Parent.NextSibling(next);
            }

            if (next != null)
            {
                return true;
            }

            block = block.Parent;
        }

        return false;
    }

    public void VisitParagraph(Paragraph paragraph)
    {
        ArgumentNullException.ThrowIfNull(paragraph);

        if (paragraph.Children.Count == 0)
        {
            if (paragraph.Virtual)
            {
                positions[paragraph] = (output.Length, output.Length);
                return;
            }

            FlushClose(closed is Paragraph { Children.Count: 0 } ? 1 : tight ? 1 : 2);
            var parent = paragraph.Parent;
            var next = parent.NextSibling(paragraph);

            while (next is Paragraph { Virtual: true, Children.Count: 0 })
            {
                next = parent.NextSibling(next);
            }

            var ownLine = (parent is not Document && !(parent is ListItem && parent.Children.Count == 1) && HasFollowing(paragraph)) || (parent is Document && next != null);

            if (AtBlank && parent is not Document)
            {
                output.Append(delimiter.TrimEnd());
            }

            positions[paragraph] = (output.Length, output.Length);

            if (ownLine)
            {
                output.Append('\n');
            }

            CloseBlock(paragraph);
            return;
        }

        FlushClose(tight ? 1 : 2);
        var start = ContentStart();
        RenderInlines(paragraph.Children);
        positions[paragraph] = (start, output.Length);
        CloseBlock(paragraph);
    }

    private int ContentStart()
    {
        if (delimiter.Length > 0 && AtBlank)
        {
            output.Append(delimiter);
        }

        return output.Length;
    }

    public void VisitHeading(Heading heading)
    {
        ArgumentNullException.ThrowIfNull(heading);

        if (heading is SetExtHeading && heading.Children.Count > 0)
        {
            FlushClose(tight ? 1 : 2);
            var contentStart = ContentStart();
            RenderInlines(heading.Children);
            positions[heading] = (contentStart, output.Length);
            var lastLine = LastSourceLine(heading);
            EnsureNewLine();
            Write(Regex.IsMatch(lastLine, "^[=-]+$") ? lastLine : new string(heading.Level == 1 ? '=' : '-', 3));
            CloseBlock(heading);
            return;
        }

        Write(new string('#', heading.Level) + (heading.Children.Count > 0 ? " " : string.Empty));
        var start = output.Length;
        inHeading = true;
        RenderInlines(heading.Children);
        inHeading = false;
        positions[heading] = (start, output.Length);
        CloseBlock(heading);
    }

    private string LastSourceLine(Block block)
    {
        var slice = Slice(block.SourceStart, block.SourceEnd);
        var line = slice[(slice.LastIndexOf('\n') + 1)..];
        return line.TrimStart(' ', '\t', '>').TrimEnd();
    }

    public void VisitThematicBreak(ThematicBreak thematicBreak)
    {
        ArgumentNullException.ThrowIfNull(thematicBreak);
        var line = thematicBreak.Pristine ? Slice(thematicBreak.SourceStart, thematicBreak.SourceEnd).Trim() : string.Empty;
        Write(line.Length > 0 && !line.Contains('\n', StringComparison.Ordinal) ? line : "---");
        positions[thematicBreak] = (output.Length, output.Length);
        CloseBlock(thematicBreak);
    }

    public void VisitBlockQuote(BlockQuote blockQuote)
    {
        ArgumentNullException.ThrowIfNull(blockQuote);
        WrapBlock("> ", null, blockQuote, () => RenderBlocks(blockQuote.Children));
    }

    public void VisitUnorderedList(UnorderedList unorderedList)
    {
        ArgumentNullException.ThrowIfNull(unorderedList);
        RenderList(unorderedList);
    }

    public void VisitOrderedList(OrderedList orderedList)
    {
        ArgumentNullException.ThrowIfNull(orderedList);
        RenderList(orderedList);
    }

    private void RenderList(List list)
    {
        var index = 0;

        foreach (var item in list.Children.OfType<ListItem>())
        {
            FlushClose(index > 0 ? list.Tight ? 1 : 2 : tight ? 1 : 2);

            var indent = string.Empty;
            var marker = Marker(list, item, index);
            var padding = Math.Max(list.Padding, marker.Length + 1);
            var firstDelim = indent + marker + new string(' ', padding - marker.Length);
            var continuation = new string(' ', indent.Length + padding);
            var current = item;
            WrapBlock(continuation, firstDelim, item, () =>
            {
                var previousTight = tight;
                tight = list.Tight;
                RenderListItem(current);
                tight = previousTight;
            });
            index++;
        }
    }

    private string Marker(List list, ListItem item, int index)
    {
        if (list is OrderedList ordered)
        {
            var number = ordered.Start + index;

            if (item.Pristine && list.Pristine && item.SourceStart >= 0)
            {
                var digits = 0;

                while (item.SourceStart + digits < source.Length && char.IsDigit(source[item.SourceStart + digits]))
                {
                    digits++;
                }

                if (digits > 0)
                {
                    number = int.Parse(source.AsSpan(item.SourceStart, digits), CultureInfo.InvariantCulture);
                }
            }

            return number.ToString(CultureInfo.InvariantCulture) + (ordered.Delimiter ?? ".");
        }

        return (list.Marker is '-' or '*' or '+' ? list.Marker : '-').ToString();
    }

    private void RenderListItem(ListItem item)
    {
        if (item.Checked is { } isChecked)
        {
            output.Append(isChecked ? "[x] " : "[ ] ");
        }

        if (item.Children.Count == 0)
        {
            positions[item] = (output.Length, output.Length);
            return;
        }

        RenderBlocks(item.Children);
    }

    public void VisitListItem(ListItem listItem)
    {
        ArgumentNullException.ThrowIfNull(listItem);
        RenderListItem(listItem);
    }

    public void VisitFencedCodeBlock(FencedCodeBlock fencedCodeBlock)
    {
        ArgumentNullException.ThrowIfNull(fencedCodeBlock);
        var fence = fencedCodeBlock.Delimiter ?? "```";
        var longest = LongestRun(fencedCodeBlock.Value, fence[0]);

        if (longest >= fence.Length)
        {
            fence = new string(fence[0], longest + 1);
        }

        Write(fence + (fencedCodeBlock.Info ?? string.Empty));
        var start = output.Length + 1 + delimiter.Length;
        prefixes[fencedCodeBlock] = delimiter;
        WriteLines(fencedCodeBlock.Value);
        positions[fencedCodeBlock] = (start, output.Length);

        if (fencedCodeBlock.Closed)
        {
            output.Append('\n');
            Write(fence);
        }

        CloseBlock(fencedCodeBlock);
    }

    private static int LongestRun(string value, char ch)
    {
        var longest = 0;
        var run = 0;

        foreach (var current in value)
        {
            run = current == ch ? run + 1 : 0;
            longest = Math.Max(longest, run);
        }

        return longest;
    }

    private void WriteLines(string value)
    {
        if (value.Length == 0)
        {
            return;
        }

        var lines = value.Split('\n');
        var count = lines.Length > 1 && lines[^1].Length == 0 ? lines.Length - 1 : lines.Length;

        for (var index = 0; index < count; index++)
        {
            output.Append('\n');

            if (lines[index].Length > 0)
            {
                output.Append(delimiter).Append(lines[index]);
            }
            else
            {
                output.Append(delimiter.TrimEnd());
            }
        }
    }

    public void VisitIndentedCodeBlock(IndentedCodeBlock codeBlock)
    {
        ArgumentNullException.ThrowIfNull(codeBlock);
        FlushClose(tight ? 1 : 2);
        var previous = delimiter;
        delimiter += "    ";
        prefixes[codeBlock] = delimiter;
        var lines = codeBlock.Value.Split('\n');
        var count = lines.Length > 1 && lines[^1].Length == 0 ? lines.Length - 1 : lines.Length;
        var start = -1;

        for (var index = 0; index < count; index++)
        {
            if (index > 0)
            {
                output.Append('\n');
            }

            if (lines[index].Length > 0)
            {
                output.Append(AtBlank ? delimiter : "    ");
                start = start < 0 ? output.Length : start;
                output.Append(lines[index]);
            }
            else if (AtBlank)
            {
                output.Append(delimiter.TrimEnd());
            }
        }

        positions[codeBlock] = (start < 0 ? output.Length : start, output.Length);
        delimiter = previous;
        CloseBlock(codeBlock);
    }

    public void VisitHtmlBlock(HtmlBlock htmlBlock)
    {
        ArgumentNullException.ThrowIfNull(htmlBlock);
        FlushClose(tight ? 1 : 2);
        var lines = htmlBlock.Value.Split('\n');

        for (var index = 0; index < lines.Length; index++)
        {
            if (index > 0)
            {
                output.Append('\n');
            }

            if (AtBlank)
            {
                output.Append(lines[index].Length > 0 ? delimiter : delimiter.TrimEnd());
            }

            output.Append(lines[index]);
        }

        positions[htmlBlock] = (output.Length, output.Length);
        CloseBlock(htmlBlock);
    }

    public void VisitTable(Table table)
    {
        ArgumentNullException.ThrowIfNull(table);
        FlushClose(tight ? 1 : 2);

        if (table.Rows.Count == 0)
        {
            return;
        }

        var header = table.Rows[0];
        inTable = true;
        WriteRow(header);
        output.Append('\n').Append(delimiter);
        var original = DelimiterRow(table);

        if (original != null && header.Cells.All(cell => cell.Pristine) && original.Trim().Trim('|').Split('|').Length == header.Cells.Count)
        {
            output.Append(original);
        }
        else
        {
            output.Append('|');

            foreach (var cell in header.Cells)
            {
                output.Append(cell.Alignment switch
                {
                    TableCellAlignment.Left => " :-- |",
                    TableCellAlignment.Center => " :-: |",
                    TableCellAlignment.Right => " --: |",
                    _ => " --- |"
                });
            }
        }

        foreach (var row in table.Rows.Skip(1))
        {
            output.Append('\n');
            WriteRow(row);
        }

        inTable = false;
        CloseBlock(table);
    }

    private string? DelimiterRow(Table table)
    {
        if (table.SourceEnd <= table.SourceStart || table.SourceEnd > source.Length)
        {
            return null;
        }

        var lines = source[table.SourceStart..table.SourceEnd].Split('\n');
        return lines.Length > 1 ? lines[1].TrimStart(' ', '\t', '>') : null;
    }

    private void WriteRow(TableRow row)
    {
        Emit("|");

        foreach (var cell in row.Cells)
        {
            output.Append(' ');
            var start = output.Length;
            RenderInlines(cell.Children);
            positions[cell] = (start, output.Length);
            output.Append(" |");
        }
    }

    public void VisitTableHeaderRow(TableHeaderRow header)
    {
        ArgumentNullException.ThrowIfNull(header);
        WriteRow(header);
    }

    public void VisitTableRow(TableRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        WriteRow(row);
    }

    public void VisitTableCell(TableCell cell)
    {
        ArgumentNullException.ThrowIfNull(cell);
        RenderInlines(cell.Children);
    }

    private readonly HashSet<Text> escapeLineStart = new(ReferenceEqualityComparer.Instance);

    private void RenderInlines(IReadOnlyList<Inline> inlines)
    {
        MarkLineStarts(inlines);
        atInlineLineStart = true;

        foreach (var inline in inlines)
        {
            inline.Accept(this);
        }
    }

    private void MarkLineStarts(IReadOnlyList<Inline> inlines)
    {
        var flat = new List<Inline>();
        Collect(inlines, flat);
        var lineStart = true;

        for (var index = 0; index < flat.Count; index++)
        {
            if (flat[index] is LineBreak or SoftLineBreak)
            {
                lineStart = true;
                continue;
            }

            if (!lineStart || flat[index] is not Text first)
            {
                lineStart = false;
                continue;
            }

            var line = new StringBuilder();

            for (var next = index; next < flat.Count && flat[next] is not (LineBreak or SoftLineBreak); next++)
            {
                line.Append(flat[next] is Text text ? text.Value : "x");
            }

            if (!inHeading && LineStart.IsMatch(line.ToString()))
            {
                escapeLineStart.Add(first);
            }

            lineStart = false;
        }
    }

    private static void Collect(IReadOnlyList<Inline> inlines, List<Inline> flat)
    {
        foreach (var inline in inlines)
        {
            if (inline is InlineContainer container)
            {
                Collect(container.Children, flat);
            }
            else
            {
                flat.Add(inline);
            }
        }
    }

    private bool afterBreak;

    public void VisitText(Text text)
    {
        ArgumentNullException.ThrowIfNull(text);
        int start;

        if (Verbatim(text) && !escapeLineStart.Contains(text))
        {
            var slice = Slice(text.SourceStart, text.SourceEnd);

            if (afterBreak)
            {
                var extra = Math.Max(0, SpacesBefore(text.SourceStart) - (delimiter.Length - delimiter.TrimEnd().Length));
                var indent = new string(' ', extra);
                start = Emit(indent + (extra < 4 && LineStart.IsMatch(slice) ? "\\" + slice : slice)) + extra;
            }
            else
            {
                start = Emit(slice);
            }
        }
        else
        {
            var lineStart = AtLineStart();
            escaped[text] = lineStart;
            start = Emit(Escape(text.Value, lineStart));
        }

        afterBreak = false;
        atInlineLineStart = false;
        positions[text] = (start, output.Length);
    }

    private int SpacesBefore(int offset)
    {
        var count = 0;

        while (offset - count - 1 >= 0 && source[offset - count - 1] is ' ' or '\t')
        {
            count++;
        }

        return count;
    }

    private static readonly Regex LineStart = new(@"^([\-*+](?=[ \t]|$)|>|#{1,6}(?=[ \t]|$)|=+[ \t]*$|(?:[\-*_][ \t]*){3,}$|\d{1,9}[.)](?=[ \t]|$)|(?:`{3,}|~{3,}))", RegexOptions.Compiled);

    private bool atInlineLineStart;

    private bool AtLineStart() => atInlineLineStart && !inHeading;

    private static readonly Regex Special = new(@"[`*\\~\[\]_<|]", RegexOptions.Compiled);

    private int EscapedOffset(string value, bool lineStart, int valueOffset)
    {
        var escaped = Escape(value, lineStart);
        var i = 0;
        var j = 0;

        while (i < valueOffset && j < escaped.Length)
        {
            if (escaped[j] != value[i])
            {
                j++;
                continue;
            }

            if (value[i] == '\\' && j + 1 < escaped.Length && escaped[j + 1] == '\\')
            {
                j++;
            }

            i++;
            j++;
        }

        return j;
    }

    internal string Escape(string value, bool lineStart)
    {
        var escaped = Special.Replace(value, match =>
        {
            var index = match.Index;

            if (match.Value == "_" && index > 0 && index + 1 < value.Length && char.IsLetterOrDigit(value[index - 1]) && char.IsLetterOrDigit(value[index + 1]))
            {
                return match.Value;
            }

            if (match.Value == "|" && !inTable)
            {
                return match.Value;
            }

            return "\\" + match.Value;
        });

        if (lineStart && LineStart.IsMatch(escaped))
        {
            var digits = Regex.Match(escaped, @"^\d{1,9}(?=[.)])");
            escaped = digits.Success ? escaped[..digits.Length] + "\\" + escaped[digits.Length..] : "\\" + escaped;
        }

        afterBreak = false;
        return escaped;
    }

    public void VisitEmphasis(Emphasis emphasis)
    {
        ArgumentNullException.ThrowIfNull(emphasis);
        RenderMark(emphasis, emphasis.Marker is { } marker ? new string(marker, 1) : DelimiterOf(emphasis, "*", 1));
    }

    public void VisitStrong(Strong strong)
    {
        ArgumentNullException.ThrowIfNull(strong);
        RenderMark(strong, strong.Marker is { } marker ? new string(marker, 2) : DelimiterOf(strong, "**", 2));
    }

    public void VisitStrikethrough(Strikethrough strikethrough)
    {
        ArgumentNullException.ThrowIfNull(strikethrough);
        var run = strikethrough.Tildes ?? LeadingRun(strikethrough, '~');
        RenderMark(strikethrough, run is 1 or 2 ? new string('~', run) : "~~");
    }

    private string DelimiterOf(InlineContainer inline, string fallback, int length)
    {
        if (inline.SourceEnd > inline.SourceStart && inline.SourceStart + length <= source.Length)
        {
            var ch = source[inline.SourceStart];

            if (ch is '*' or '_')
            {
                return new string(ch, length);
            }
        }

        return fallback;
    }

    private int LeadingRun(Inline inline, char ch)
    {
        var run = 0;

        while (inline.SourceStart >= 0 && inline.SourceStart + run < source.Length && inline.SourceStart + run < inline.SourceEnd && source[inline.SourceStart + run] == ch)
        {
            run++;
        }

        return run;
    }

    private readonly Dictionary<Text, int> markerBefore = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<Text, int> markerAfter = new(ReferenceEqualityComparer.Instance);

    private int MarkerLengthBefore(Text text) => markerBefore.TryGetValue(text, out var length) ? length : 0;

    private int MarkerLengthAfter(Text text) => markerAfter.TryGetValue(text, out var length) ? length : 0;

    private void RenderMark(InlineContainer inline, string marker)
    {
        if (Verbatim(inline) && inline.Children.Count == 0)
        {
            var start = Emit(Slice(inline.SourceStart, inline.SourceEnd));
            positions[inline] = (start, output.Length);
            return;
        }

        if (Verbatim(inline))
        {
            var (contentStart, contentEnd) = VerbatimContent(inline);
            Emit(Slice(inline.SourceStart, contentStart));
            var start = output.Length;
            RenderInlines(inline.Children);
            positions[inline] = (start, output.Length);
            output.Append(Slice(contentEnd, inline.SourceEnd));
            return;
        }

        var (leading, body, trailing) = ExpelWhitespace(inline);

        if (leading.Length > 0)
        {
            Emit(leading);
        }

        Emit(marker);
        var bodyStart = output.Length;
        RenderInlines(body);
        positions[inline] = (bodyStart, output.Length);
        output.Append(marker);

        if (body.Count > 0 && body[0] is Text firstBody)
        {
            markerBefore[firstBody] = marker.Length;
        }

        if (body.Count > 0 && body[^1] is Text lastBody)
        {
            markerAfter[lastBody] = marker.Length;
        }

        if (trailing.Length > 0)
        {
            output.Append(trailing);
        }
    }

    private (int Start, int End) VerbatimContent(InlineContainer inline)
    {
        if (inline.Children.Count == 0)
        {
            return (inline.SourceStart, inline.SourceEnd);
        }

        return (inline.Children[0].SourceStart, inline.Children[^1].SourceEnd);
    }

    private readonly Dictionary<Text, (Text Replacement, int Leading, string Trailing)> expelled = new(ReferenceEqualityComparer.Instance);

    private (string Leading, IReadOnlyList<Inline> Body, string Trailing) ExpelWhitespace(InlineContainer inline)
    {
        var leading = string.Empty;
        var trailing = string.Empty;
        var body = new List<Inline>(inline.Children);

        if (body.Count > 0 && body[0] is Text first && first.Value.Length > first.Value.TrimStart().Length)
        {
            leading = first.Value[..(first.Value.Length - first.Value.TrimStart().Length)];
            var replacement = new Text(first.Value.TrimStart()) { SourceStart = first.SourceStart + leading.Length, SourceEnd = first.SourceEnd, Pristine = false };
            body[0] = replacement;
            expelled[first] = (replacement, leading.Length, string.Empty);
        }

        if (body.Count > 0 && body[^1] is Text last && last.Value.Length > last.Value.TrimEnd().Length)
        {
            trailing = last.Value[last.Value.TrimEnd().Length..];
            var replacement = new Text(last.Value.TrimEnd()) { SourceStart = last.SourceStart, SourceEnd = last.SourceEnd - trailing.Length, Pristine = false };
            var existing = expelled.TryGetValue(last, out var earlier) ? earlier : default;
            body[^1] = replacement;
            expelled[last] = (replacement, existing.Leading, trailing);
        }

        return (leading, body, trailing);
    }

    public void VisitCode(Code code)
    {
        ArgumentNullException.ThrowIfNull(code);
        if (Verbatim(code))
        {
            var start = Emit(Slice(code.SourceStart, code.SourceEnd));
            positions[code] = (start, output.Length);
            return;
        }

        var ticks = new string('`', Math.Max(code.Ticks ?? 1, LongestRun(code.Value, '`') + 1));
        var padded = code.Value.Length > 0 && (code.Value[0] == '`' || code.Value[^1] == '`' || (code.Value[0] == ' ' && code.Value[^1] == ' ' && code.Value.Trim().Length > 0));
        Emit(ticks);

        if (padded)
        {
            output.Append(' ');
        }

        var contentStart = output.Length;
        output.Append(code.Value);
        positions[code] = (contentStart, output.Length);

        if (padded)
        {
            output.Append(' ');
        }

        output.Append(ticks);
    }

    public void VisitLink(Link link)
    {
        ArgumentNullException.ThrowIfNull(link);
        RenderLink(link, link.Destination, link.Title, image: false);
    }

    public void VisitImage(Image image)
    {
        ArgumentNullException.ThrowIfNull(image);
        RenderLink(image, image.Destination, image.Title, image: true);
    }

    private void RenderLink(InlineContainer inline, string? destination, string? title, bool image)
    {
        if (Verbatim(inline) && inline.Children.Count == 0)
        {
            var wholeStart = Emit(Slice(inline.SourceStart, inline.SourceEnd));
            positions[inline] = (wholeStart, output.Length);
            return;
        }

        if (Verbatim(inline))
        {
            var (contentStart, contentEnd) = VerbatimContent(inline);
            Emit(Slice(inline.SourceStart, contentStart));
            var verbatimStart = output.Length;
            RenderInlines(inline.Children);
            positions[inline] = (verbatimStart, output.Length);
            output.Append(Slice(contentEnd, inline.SourceEnd));
            return;
        }

        if (!image && inline is Link { Suffix: { } suffix } reference)
        {
            Emit("[");
            var referenceStart = output.Length;
            RenderInlines(reference.Children);
            positions[inline] = (referenceStart, output.Length);
            output.Append(suffix);
            return;
        }

        if (!image && ((inline is Link { Autolink: true }) || (inline.SourceEnd > inline.SourceStart && inline.SourceStart < source.Length && source[inline.SourceStart] == '<')) && inline.Children is [Text only] && only.Value == destination)
        {
            Emit("<");
            var autolinkStart = output.Length;
            output.Append(destination);
            positions[inline] = (autolinkStart, output.Length);
            output.Append('>');
            return;
        }

        Emit(image ? "![" : "[");
        var start = output.Length;
        RenderInlines(inline.Children);
        positions[inline] = (start, output.Length);
        output.Append("](");
        var target = destination ?? string.Empty;
        output.Append(target.Length == 0 || target.Any(ch => ch is ' ' or '(' or ')' or '\n') ? "<" + target.Replace("<", "\\<", StringComparison.Ordinal).Replace(">", "\\>", StringComparison.Ordinal) + ">" : target);

        if (!string.IsNullOrEmpty(title))
        {
            output.Append(" \"").Append(title.Replace("\"", "\\\"", StringComparison.Ordinal)).Append('"');
        }

        output.Append(')');
    }

    public void VisitHtmlInline(HtmlInline html)
    {
        ArgumentNullException.ThrowIfNull(html);
        var start = Emit(html.Value ?? string.Empty);
        positions[html] = (start, output.Length);
    }

    public void VisitLineBreak(LineBreak lineBreak)
    {
        ArgumentNullException.ThrowIfNull(lineBreak);
        var backslash = lineBreak.Backslash ?? (lineBreak.SourceStart >= 0 && lineBreak.SourceStart < lineBreak.SourceEnd && lineBreak.SourceStart < source.Length && source[lineBreak.SourceStart] == '\\');
        var start = Emit(backslash ? "\\" : "  ");
        output.Append('\n');
        Emit(string.Empty);
        positions[lineBreak] = (start, output.Length);
        afterBreak = true;
        atInlineLineStart = true;
    }

    public void VisitSoftLineBreak(SoftLineBreak softLineBreak)
    {
        ArgumentNullException.ThrowIfNull(softLineBreak);
        positions[softLineBreak] = (output.Length, output.Length);
        output.Append('\n');
        afterBreak = true;
        atInlineLineStart = true;
    }
}
