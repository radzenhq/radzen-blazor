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

    private readonly bool verbatimBlocks;

    private MarkdownWriter(string source, string delimiter, bool verbatimBlocks)
    {
        this.source = source;
        this.delimiter = delimiter;
        this.verbatimBlocks = verbatimBlocks;
    }

    public IReadOnlyDictionary<INode, (int Start, int End)> Positions => positions;

    public string Text => output.ToString();

    public static string Write(Document document, string source) => Serialize(document, source, verbatimBlocks: false).Text;

    public static MarkdownWriter Preserve(Document document, string source) => Serialize(document, source, verbatimBlocks: true);

    private static MarkdownWriter Serialize(Document document, string source, bool verbatimBlocks)
    {
        var writer = new MarkdownWriter(source, string.Empty, verbatimBlocks);
        document.Accept(writer);
        return writer;
    }

    private bool AtBlank => output.Length == 0 || output[^1] == '\n';

    private static bool IsPristine(Block block) => block.Pristine && block switch
    {
        BlockContainer container => container.Children.All(IsPristine),
        Table table => table.Rows.All(row => row.Cells.All(cell => cell.Pristine)),
        _ => true
    };

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

    private sealed class Rendering
    {
        public int Leading;
        public int LeadingAt;
        public int[]? LeadingMap;
        public int Trailing;
        public int TrailingAt;
        public int[]? TrailingMap;
        public int[]? Map;
    }

    private readonly Dictionary<Text, Rendering> renderings = new(ReferenceEqualityComparer.Instance);

    public int OffsetWithin(Text text, int valueOffset)
    {
        var start = positions[text].Start;

        if (!renderings.TryGetValue(text, out var rendering))
        {
            return start + Math.Clamp(valueOffset, 0, text.Value.Length);
        }

        if (valueOffset < rendering.Leading)
        {
            return rendering.LeadingMap != null ? rendering.LeadingAt + rendering.LeadingMap[valueOffset] : start;
        }

        var bodyLength = text.Value.Length - rendering.Leading - rendering.Trailing;

        if (valueOffset > rendering.Leading + bodyLength)
        {
            return rendering.TrailingMap != null ? rendering.TrailingAt + rendering.TrailingMap[valueOffset - rendering.Leading - bodyLength] : positions[text].End;
        }

        return rendering.Map != null ? start + rendering.Map[valueOffset - rendering.Leading] : start;
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

    private int LazySeparator()
    {
        if (!tight)
        {
            return 2;
        }

        if (closed is BlockQuote)
        {
            EnsureNewLine();
            output.Append(delimiter).Append(">\n");
            closed = null;
            return 1;
        }

        return closed is List or ListItem && EndsWithParagraph(closed) ? 2 : 1;
    }

    private static bool EndsWithParagraph(Block block)
    {
        while (block is BlockContainer { LastChild: { } last })
        {
            block = last;
        }

        return block is Paragraph { Children.Count: > 0 };
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

    private bool definitionsFollow;

    public void VisitDocument(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);
        Block? previous = null;
        var pending = new List<Paragraph>();
        definitionsFollow = document.LinkReferenceDefinitions.Count > 0;

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

            if (verbatim && previousVerbatim && source.AsSpan(previous!.SourceEnd, block.SourceStart - previous.SourceEnd).IsWhiteSpace() && BlankLines.Placeholders(source, previous.SourceEnd, block.SourceStart, true, true).Count == pending.Count)
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

        FlushClose(LazySeparator());
        var start = ContentStart();
        RenderInlines(paragraph.Children, "\n", lineStart: true, blockEnd: true);
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
            FlushClose(LazySeparator());
            var contentStart = ContentStart();
            RenderInlines(heading.Children, "\n", lineStart: true, blockEnd: true);
            positions[heading] = (contentStart, output.Length);
            var underline = ((SetExtHeading)heading).Underline ?? string.Empty;
            EnsureNewLine();
            Write(Regex.IsMatch(underline, "^[=-]+$") ? underline : new string(heading.Level == 1 ? '=' : '-', 3));
            CloseBlock(heading);
            return;
        }

        Write(new string('#', heading.Level) + (heading.Children.Count > 0 ? " " : string.Empty));
        var start = output.Length;
        var enclosing = constructs;
        constructs |= Construct.HeadingAtx;
        RenderInlines(heading.Children, "\n", blockEnd: true);
        constructs = enclosing;
        positions[heading] = (start, output.Length);
        CloseBlock(heading);
    }

    public void VisitThematicBreak(ThematicBreak thematicBreak)
    {
        ArgumentNullException.ThrowIfNull(thematicBreak);
        var line = thematicBreak.Line ?? string.Empty;
        Write(line.Length > 0 && !line.Contains('\n', StringComparison.Ordinal) ? line : "---");
        positions[thematicBreak] = (output.Length, output.Length);
        CloseBlock(thematicBreak);
    }

    public void VisitBlockQuote(BlockQuote blockQuote)
    {
        ArgumentNullException.ThrowIfNull(blockQuote);
        WrapBlock("> ", null, blockQuote, () =>
        {
            var previousTight = tight;
            tight = false;
            RenderBlocks(blockQuote.Children);
            tight = previousTight;
        });
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
                RenderListItem(current, firstDelim);
                tight = previousTight;
            });
            index++;
        }
    }

    private string Marker(List list, ListItem item, int index)
    {
        if (list is OrderedList ordered)
        {
            var number = item.Pristine && list.Pristine && item.Number is { } original ? original : ordered.Start + index;
            return number.ToString(CultureInfo.InvariantCulture) + (ordered.Delimiter ?? ".");
        }

        return (list.Marker is '-' or '*' or '+' ? list.Marker : '-').ToString();
    }

    private void RenderListItem(ListItem item, string marker)
    {
        if (item.Checked is { } isChecked)
        {
            output.Append(isChecked ? "[x] " : "[ ] ");
        }

        if (item.Children.Count > 0 && (item.Checked != null ? item.Children[0] is not (Paragraph or Heading) : item.Children[0] is ThematicBreak))
        {
            output.Append('\n');
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
        RenderListItem(listItem, string.Empty);
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
        var closed = fencedCodeBlock.Closed || HasFollowing(fencedCodeBlock) || definitionsFollow;
        WriteLines(fencedCodeBlock.Value, keepTrailing: !closed);
        positions[fencedCodeBlock] = (start, output.Length);

        if (closed)
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

    private void WriteLines(string value, bool keepTrailing = false)
    {
        if (value.Length == 0)
        {
            return;
        }

        var lines = value.Split('\n');
        var count = lines.Length > 1 && lines[^1].Length == 0 && !keepTrailing ? lines.Length - 1 : lines.Length;

        for (var index = 0; index < count; index++)
        {
            output.Append('\n');

            if (lines[index].Length > 0)
            {
                output.Append(delimiter).Append(lines[index]);
            }
            else if (!keepTrailing || index < count - 1)
            {
                output.Append(delimiter.TrimEnd());
            }
        }
    }

    public void VisitIndentedCodeBlock(IndentedCodeBlock codeBlock)
    {
        ArgumentNullException.ThrowIfNull(codeBlock);
        var fence = closed is List or ListItem ? new string('`', Math.Max(3, LongestRun(codeBlock.Value, '`') + 1)) : null;
        FlushClose(tight ? 1 : 2);
        var previous = delimiter;

        if (fence != null)
        {
            Write(fence);
            output.Append('\n');
        }
        else
        {
            delimiter += "    ";
        }

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

        if (fence != null)
        {
            output.Append('\n');
            Write(fence);
        }

        delimiter = previous;
        CloseBlock(codeBlock);
    }

    public void VisitHtmlBlock(HtmlBlock htmlBlock)
    {
        ArgumentNullException.ThrowIfNull(htmlBlock);
        var afterList = closed is List or ListItem;
        FlushClose(tight ? 1 : 2);
        var lines = htmlBlock.Value.Split('\n');

        if (afterList)
        {
            var indent = lines.Where(line => line.Trim().Length > 0).Select(line => line.Length - line.TrimStart(' ').Length).DefaultIfEmpty(0).Min();
            lines = lines.Select(line => line.Length >= indent ? line[indent..] : line.TrimStart(' ')).ToArray();
        }

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
        FlushClose(LazySeparator());

        if (table.Rows.Count == 0)
        {
            return;
        }

        var header = table.Rows[0];
        WriteRow(header);
        output.Append('\n').Append(delimiter);
        var original = table.DelimiterLine;

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

        CloseBlock(table);
    }

    private void WriteRow(TableRow row)
    {
        Emit("|");
        var enclosing = constructs;
        constructs |= Construct.TableCell;

        foreach (var cell in row.Cells)
        {
            output.Append(' ');
            var start = output.Length;
            RenderInlines(cell.Children, " ");
            positions[cell] = (start, output.Length);
            output.Append(" |");
        }

        constructs = enclosing;
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
        RenderInlines(cell.Children, " ");
    }

    private int inlineDepth;

    private void RenderInlines(IReadOnlyList<Inline> inlines, string after = "\n", bool lineStart = false, bool blockEnd = false)
    {
        inlines = Normalize(inlines);
        var enclosing = constructs;

        if (inlineDepth == 0)
        {
            constructs |= Construct.Phrasing;
            atInlineLineStart = lineStart;
            contentAtLineStart = lineStart;
            lineStartAt = output.Length;
        }

        inlineDepth++;
        var trailing = inlines.Count;

        while (blockEnd && trailing > 0 && inlines[trailing - 1] is LineBreak)
        {
            trailing--;
        }

        for (var index = 0; index < inlines.Count; index++)
        {
            following = Peek(inlines, index + 1, after);
            endsBlock = blockEnd && index >= trailing - 1;

            if (inlines[index] is Text text && index + 1 < inlines.Count && inlines[index + 1] is SoftLineBreak)
            {
                RenderingOf(text).Trailing = text.Value.Length - text.Value.TrimEnd(' ', '\t').Length;
            }

            inlines[index].Accept(this);
        }

        inlineDepth--;
        constructs = enclosing;
    }

    private string following = "\n";

    private bool endsBlock;

    private static string Peek(IReadOnlyList<Inline> inlines, int index, string after)
    {
        for (; index < inlines.Count; index++)
        {
            switch (inlines[index])
            {
                case Text { Value.Length: > 0 } text:
                    return text.Value[..1];
                case Emphasis emphasis:
                    return Lead(emphasis, emphasis.Marker ?? '*');
                case Strong strong:
                    return Lead(strong, strong.Marker ?? '*');
                case Strikethrough strikethrough:
                    return Lead(strikethrough, '~');
                case Code:
                    return "`";
                case Link { Autolink: true }:
                    return "<";
                case Link:
                    return "[";
                case Image:
                    return "!";
                case HtmlInline { Value.Length: > 0 } html:
                    return html.Value[..1];
                case LineBreak lineBreak:
                    return lineBreak.Backslash == true ? "\\" : " ";
                case SoftLineBreak:
                    return "\n";
            }
        }

        return after;
    }

    private static string Lead(InlineContainer container, char marker) => container.Children is [Text { Value.Length: > 0 } first, ..] && char.IsWhiteSpace(first.Value[0]) ? first.Value[..1] : marker.ToString();

    private int lineStartAt;

    private bool contentAtLineStart;

    private string Before() => (contentAtLineStart ? "\n" : string.Empty) + output.ToString(lineStartAt, output.Length - lineStartAt);

    // CommonMark 0.31.2, 6.2 Emphasis: a delimiter run next to punctuation is only flanking when the other side is whitespace or punctuation
    private static bool Flanks(char outside, char inside) => !(IsPunctuation(inside) && !char.IsWhiteSpace(outside) && !IsPunctuation(outside) && outside != '\0');

    // CommonMark 0.31.2, 2.1 Characters and lines: Unicode punctuation is the P categories plus ASCII symbols
    private static bool IsPunctuation(char ch) => char.IsPunctuation(ch) || (ch < 128 && char.IsSymbol(ch));

    public void VisitText(Text text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var rendering = RenderingOf(text);
        var value = text.Value[rendering.Leading..^rendering.Trailing];

        if (atInlineLineStart && rendering.Leading == 0)
        {
            value = value.TrimStart(' ', '\t');
            rendering.Leading = text.Value.Length - rendering.Trailing - value.Length;
        }

        var start = Emit(Safe(value, Before(), following, out rendering.Map));
        atInlineLineStart = false;
        positions[text] = (start, output.Length);
    }

    private Rendering RenderingOf(Text text)
    {
        if (!renderings.TryGetValue(text, out var rendering))
        {
            rendering = new Rendering();
            renderings[text] = rendering;
        }

        return rendering;
    }

    private bool atInlineLineStart;

    [Flags]
    private enum Construct
    {
        None = 0,
        Phrasing = 1,
        HeadingAtx = 2,
        TableCell = 4,
        Label = 8,
        DestinationLiteral = 16,
        DestinationRaw = 32,
        Title = 64
    }

    private const Construct Spans = Construct.DestinationLiteral | Construct.DestinationRaw | Construct.Title;

    private Construct constructs;

    private sealed record Unsafe(string Character, string? Before = null, string? After = null, bool AtBreak = false, Construct In = Construct.None, Construct NotIn = Construct.None)
    {
        public Regex Pattern { get; } = new((AtBreak || Before != null ? "(?<=" + (AtBreak ? @"[\r\n][\t ]*" : string.Empty) + (Before ?? string.Empty) + ")" : string.Empty) + (Character.Length == 1 ? Regex.Escape(Character) : "(?:" + Character + ")") + (After != null ? "(?=" + After + ")" : string.Empty));

        public bool HasBefore => AtBreak || Before != null;

        public bool HasAfter => After != null;
    }

    // CommonMark 0.31.2 sections 4 to 6 and GFM 0.29-gfm sections 4.10 and 6.5: the characters that start or end a construct in each context
    private static readonly Unsafe[] Rules =
    [
        new("!", After: @"\[", In: Construct.Phrasing, NotIn: Spans),
        new("\"", In: Construct.Title),
        new("#", AtBreak: true),
        new("#", After: @"(?:[\r\n]|$)", In: Construct.HeadingAtx),
        new("&", After: "[#A-Za-z]", In: Construct.Phrasing),
        new("(", In: Construct.DestinationRaw),
        new("(", Before: @"\]", In: Construct.Phrasing, NotIn: Spans),
        new(")", Before: @"\d+", AtBreak: true),
        new(")", In: Construct.DestinationRaw),
        new("*", After: @"[ \t\r\n*]", AtBreak: true),
        new("*", In: Construct.Phrasing, NotIn: Spans),
        new("+", After: @"[ \t\r\n]", AtBreak: true),
        new("-", After: @"[ \t\r\n-]", AtBreak: true),
        new("-", After: "[:|-]", AtBreak: true),
        new(".", Before: @"\d+", After: @"(?:[ \t\r\n]|$)", AtBreak: true),
        new(":", After: "-", AtBreak: true),
        new("<", After: "[!/?A-Za-z]", AtBreak: true),
        new("<", After: "[!/?A-Za-z]", In: Construct.Phrasing, NotIn: Spans),
        new("<", In: Construct.DestinationLiteral),
        new("=", AtBreak: true),
        new(">", AtBreak: true),
        new(">", In: Construct.DestinationLiteral),
        new("[", AtBreak: true),
        new("[", In: Construct.Phrasing, NotIn: Spans),
        new("[", In: Construct.Label),
        new("\\", After: @"[\r\n]", In: Construct.Phrasing),
        new("]", In: Construct.Label),
        new("_", AtBreak: true),
        new(@"(?<![\p{L}\p{N}]_*)_|_(?!_*[\p{L}\p{N}])", In: Construct.Phrasing, NotIn: Spans),
        new("`", AtBreak: true),
        new("`", In: Construct.Phrasing, NotIn: Spans),
        new("|", After: @"[\t :-]", AtBreak: true),
        new("|", In: Construct.TableCell),
        new("~", AtBreak: true),
        new("~", In: Construct.Phrasing, NotIn: Spans)
    ];

    private string Safe(string value, string before, string after, out int[] map)
    {
        var whole = before + value + after;
        var infos = new Dictionary<int, (bool Before, bool After)>();

        foreach (var rule in Rules)
        {
            if ((rule.In != Construct.None && (constructs & rule.In) == 0) || (constructs & rule.NotIn) != 0)
            {
                continue;
            }

            foreach (Match match in rule.Pattern.Matches(whole))
            {
                infos[match.Index] = infos.TryGetValue(match.Index, out var info) ? (info.Before && rule.HasBefore, info.After && rule.HasAfter) : (rule.HasBefore, rule.HasAfter);
            }
        }

        var escaped = new StringBuilder();
        var start = before.Length;
        var end = whole.Length - after.Length;
        map = new int[value.Length + 1];

        for (var index = start; index < end; index++)
        {
            map[index - start] = escaped.Length;
            var ch = whole[index];

            if (ch.IsEscapable() && ((infos.TryGetValue(index, out var info) && !Skipped(index, info)) || (ch == '\\' && index + 1 < whole.Length && whole[index + 1].IsEscapable())))
            {
                escaped.Append('\\');
            }

            escaped.Append(ch);
        }

        map[value.Length] = escaped.Length;
        return escaped.ToString();

        bool Skipped(int position, (bool Before, bool After) info) =>
            (position + 1 < end && info.After && infos.TryGetValue(position + 1, out var next) && !next.Before && !next.After)
            || (info.Before && infos.TryGetValue(position - 1, out var previous) && !previous.Before && !previous.After);
    }

    public void VisitEmphasis(Emphasis emphasis)
    {
        ArgumentNullException.ThrowIfNull(emphasis);
        RenderMark(emphasis, new string(emphasis.Marker ?? '*', 1));
    }

    public void VisitStrong(Strong strong)
    {
        ArgumentNullException.ThrowIfNull(strong);
        RenderMark(strong, new string(strong.Marker ?? '*', 2));
    }

    public void VisitStrikethrough(Strikethrough strikethrough)
    {
        ArgumentNullException.ThrowIfNull(strikethrough);
        RenderMark(strikethrough, strikethrough.Tildes is 1 or 2 ? new string('~', strikethrough.Tildes.Value) : "~~");
    }

    private readonly List<Type> openMarks = [];


    private void RenderMark(InlineContainer inline, string marker)
    {
        openMarks.Add(inline.GetType());

        try
        {
            RenderMarkBody(inline, marker);
        }
        finally
        {
            openMarks.RemoveAt(openMarks.Count - 1);
        }
    }

    private readonly Dictionary<Code, List<(Code Original, int Offset)>> mergedCodes = new(ReferenceEqualityComparer.Instance);

    // CommonMark 0.31.2, 6.2 Emphasis and 6.1 Code spans: nested or adjacent runs of one kind cannot be told apart from a longer run, and they render the same as a single node
    private IReadOnlyList<Inline> Normalize(IReadOnlyList<Inline> inlines)
    {
        var result = new List<Inline>();

        foreach (var inline in inlines)
        {
            Add(inline);
        }

        return result;

        Inline? Last()
        {
            for (var index = result.Count - 1; index >= 0; index--)
            {
                if (result[index] is not Text { Value.Length: 0 })
                {
                    return result[index];
                }
            }

            return null;
        }

        void Replace(Inline replacement)
        {
            var index = result.Count - 1;

            while (result[index] is Text { Value.Length: 0 })
            {
                index--;
            }

            result[index] = replacement;
        }

        void Add(Inline inline)
        {
            if (inline is Emphasis or Strong or Strikethrough && openMarks.Contains(inline.GetType()))
            {
                foreach (var child in ((InlineContainer)inline).Children)
                {
                    Add(child);
                }

                return;
            }

            if (inline is Code code && Last() is Code previous)
            {
                var merged = new Code(previous.Value + code.Value) { Ticks = previous.Ticks };
                var originals = mergedCodes.TryGetValue(previous, out var known) ? known : [(previous, 0)];
                originals.Add((code, previous.Value.Length));
                mergedCodes[merged] = originals;
                Replace(merged);
                return;
            }

            if (inline is Emphasis or Strong or Strikethrough && Last() is { } last && last.GetType() == inline.GetType())
            {
                var previousMark = (InlineContainer)last;
                InlineContainer combined = inline switch
                {
                    Emphasis emphasis => new Emphasis { Marker = ((Emphasis)previousMark).Marker ?? emphasis.Marker },
                    Strong strong => new Strong { Marker = ((Strong)previousMark).Marker ?? strong.Marker },
                    _ => new Strikethrough { Tildes = ((Strikethrough)previousMark).Tildes }
                };

                foreach (var child in previousMark.Children.Concat(((InlineContainer)inline).Children).ToList())
                {
                    combined.Add(child);
                }

                Replace(combined);
                return;
            }

            result.Add(inline);
        }
    }

    private static int PunctuationPrefix(string value)
    {
        var count = 0;

        while (count < value.Length && IsPunctuation(value[count]))
        {
            count++;
        }

        return count;
    }

    private static int PunctuationSuffix(string value)
    {
        var count = 0;

        while (count < value.Length && IsPunctuation(value[^(count + 1)]))
        {
            count++;
        }

        return count;
    }

    private static Text? FirstText(IReadOnlyList<Inline> body) => body.Count == 0 ? null : body[0] switch
    {
        Text text => text,
        Emphasis or Strong or Strikethrough => FirstText(((InlineContainer)body[0]).Children),
        _ => null
    };

    private static Text? LastText(IReadOnlyList<Inline> body) => body.Count == 0 ? null : body[^1] switch
    {
        Text text => text,
        Emphasis or Strong or Strikethrough => LastText(((InlineContainer)body[^1]).Children),
        _ => null
    };

    private bool Blank(IReadOnlyList<Inline> body) => body.All(child => child switch
    {
        Text text => text.Value.Length == RenderingOf(text).Leading + RenderingOf(text).Trailing,
        Emphasis or Strong or Strikethrough => Blank(((InlineContainer)child).Children),
        _ => false
    });

    private void RenderMarkBody(InlineContainer inline, string marker)
    {
        var next = following;
        var last = endsBlock;
        var body = inline.Children.ToList();
        var breaks = new List<Inline>();

        while (body.Count > 0 && body[^1] is LineBreak)
        {
            breaks.Insert(0, body[^1]);
            body.RemoveAt(body.Count - 1);
        }

        var before = output.Length > 0 ? output[^1] : '\0';
        var after = (breaks.Count > 0 ? Peek(breaks, 0, next) : next)[0];
        var leading = Expel(FirstText(body), before, leading: true);
        var trailing = Expel(LastText(body), after, leading: false);

        if (leading.Length > 0)
        {
            var first = FirstText(body)!;
            RenderingOf(first).LeadingAt = Emit(Safe(leading, Before(), marker, out RenderingOf(first).LeadingMap));
        }

        if (Blank(body))
        {
            positions[inline] = (output.Length, output.Length);

            foreach (var node in Flatten(body))
            {
                positions[node] = (output.Length, output.Length);

                if (node is Text text)
                {
                    RenderingOf(text).TrailingAt = output.Length;
                }
            }
        }
        else
        {
            Emit(marker);
            var bodyStart = output.Length;
            RenderInlines(body, marker);
            positions[inline] = (bodyStart, output.Length);
            output.Append(marker);
        }

        if (trailing.Length > 0)
        {
            var text = LastText(body)!;
            RenderingOf(text).TrailingAt = output.Length;
            output.Append(Safe(trailing, Before(), next, out RenderingOf(text).TrailingMap));
        }

        RenderInlines(breaks, next, blockEnd: last);
    }

    private string Expel(Text? text, char outside, bool leading)
    {
        if (text == null)
        {
            return string.Empty;
        }

        var rendering = RenderingOf(text);
        var remaining = text.Value[rendering.Leading..^rendering.Trailing];
        var count = remaining.Length - (leading ? remaining.TrimStart() : remaining.TrimEnd()).Length;

        if (count == 0 && remaining.Length > 0 && !Flanks(outside, leading ? remaining[0] : remaining[^1]))
        {
            count = leading ? PunctuationPrefix(remaining) : PunctuationSuffix(remaining);
        }

        if (leading)
        {
            rendering.Leading += count;
            return remaining[..count];
        }

        rendering.Trailing += count;
        return remaining[^count..];
    }

    private static IEnumerable<Inline> Flatten(IReadOnlyList<Inline> inlines) => inlines.SelectMany(inline => inline is InlineContainer container ? Flatten(container.Children).Prepend(inline) : [inline]);

    public void VisitCode(Code code)
    {
        ArgumentNullException.ThrowIfNull(code);
        var value = code.Value.Replace('\n', ' ');
        var ticks = new string('`', Math.Max(code.Ticks ?? 1, LongestRun(value, '`') + 1));
        var padded = value.Length > 0 && (value[0] == '`' || value[^1] == '`' || (value[0] == ' ' && value[^1] == ' ' && value.Trim().Length > 0));
        Emit(ticks);

        if (padded)
        {
            output.Append(' ');
        }

        var contentStart = output.Length;
        output.Append(value);
        positions[code] = (contentStart, output.Length);

        if (mergedCodes.TryGetValue(code, out var originals))
        {
            foreach (var (original, offset) in originals)
            {
                positions[original] = (contentStart + offset, contentStart + offset + original.Value.Length);
            }
        }

        if (padded)
        {
            output.Append(' ');
        }

        output.Append(ticks);
        atInlineLineStart = false;
    }

    public void VisitLink(Link link)
    {
        ArgumentNullException.ThrowIfNull(link);
        RenderLink(link, link.Destination, link.Title, link.Suffix, image: false);
    }

    public void VisitImage(Image image)
    {
        ArgumentNullException.ThrowIfNull(image);
        RenderLink(image, image.Destination, image.Title, image.Suffix, image: true);
    }

    private void RenderLink(InlineContainer inline, string? destination, string? title, string? suffix, bool image)
    {
        var enclosing = openMarks.ToList();
        openMarks.Clear();

        try
        {
            RenderLinkBody(inline, destination, title, suffix, image);
        }
        finally
        {
            openMarks.AddRange(enclosing);
        }
    }

    private void RenderLinkBody(InlineContainer inline, string? destination, string? title, string? suffix, bool image)
    {
        atInlineLineStart = false;
        var outerStart = Emit(string.Empty);
        var enclosing = constructs;

        if (suffix != null)
        {
            Emit(image ? "![" : "[");
            var referenceStart = output.Length;
            constructs |= Construct.Label;
            RenderInlines(inline.Children, "]");
            constructs = enclosing;
            atInlineLineStart = false;
            positions[inline] = image ? (outerStart, output.Length + suffix.Length) : (referenceStart, output.Length);
            output.Append(suffix);
            return;
        }

        if (inline is Link { Autolink: true } && inline.Children is [Text only] && (only.Value == destination || "mailto:" + only.Value == destination))
        {
            Emit("<");
            var autolinkStart = output.Length;
            output.Append(only.Value);
            positions[inline] = (autolinkStart, output.Length);
            output.Append('>');
            return;
        }

        Emit(image ? "![" : "[");
        var start = output.Length;
        constructs |= Construct.Label;
        RenderInlines(inline.Children, "](");
        constructs = enclosing;
        atInlineLineStart = false;
        positions[inline] = (start, output.Length);
        output.Append("](");
        var target = (destination ?? string.Empty).Replace("\r", "%0D", StringComparison.Ordinal).Replace("\n", "%0A", StringComparison.Ordinal);

        if ((target.Length == 0 && !string.IsNullOrEmpty(title)) || Regex.IsMatch(target, @"[\u0000- \u007F]"))
        {
            constructs = enclosing | Construct.DestinationLiteral;
            output.Append('<').Append(Safe(target, "<", ">", out _)).Append('>');
        }
        else
        {
            constructs = enclosing | Construct.DestinationRaw;
            output.Append(Safe(target, "(", string.IsNullOrEmpty(title) ? ")" : " ", out _));
        }

        if (!string.IsNullOrEmpty(title))
        {
            constructs = enclosing | Construct.Title;
            output.Append(" \"").Append(Safe(title, "\"", "\"", out _)).Append('"');
        }

        constructs = enclosing;
        output.Append(')');

        if (image)
        {
            positions[inline] = (outerStart, output.Length);
        }
    }

    // CommonMark 0.31.2, 4.6 HTML blocks: only start conditions 1 to 6 may interrupt a paragraph
    private static bool StartsHtmlBlock(string? line) => line != null && Enumerable.Range(1, 6).Any(type => BlockParser.HtmlBlockOpenRegex[type].IsMatch(line));

    public void VisitHtmlInline(HtmlInline html)
    {
        ArgumentNullException.ThrowIfNull(html);
        var value = html.Value ?? string.Empty;
        // CommonMark 0.31.2, 4.4 Indented code blocks: an indented line cannot interrupt a paragraph
        var start = Emit((atInlineLineStart && StartsHtmlBlock(value) ? "\\" : string.Empty) + value.Replace("\n", "\n    ", StringComparison.Ordinal));
        positions[html] = (start, output.Length);
        atInlineLineStart = false;
    }

    public void VisitLineBreak(LineBreak lineBreak)
    {
        ArgumentNullException.ThrowIfNull(lineBreak);
        var start = Emit(lineBreak.Backslash == true && !endsBlock ? "\\" : "  ");
        output.Append('\n');
        Emit(string.Empty);
        positions[lineBreak] = (start, output.Length);
        atInlineLineStart = true;
        contentAtLineStart = true;
        lineStartAt = output.Length;
    }

    public void VisitSoftLineBreak(SoftLineBreak softLineBreak)
    {
        ArgumentNullException.ThrowIfNull(softLineBreak);

        while (output.Length > 0 && output[^1] is ' ' or '\t')
        {
            output.Length--;
        }

        positions[softLineBreak] = (output.Length, output.Length);
        output.Append('\n');
        Emit(string.Empty);
        atInlineLineStart = true;
        contentAtLineStart = true;
        lineStartAt = output.Length;
    }
}
