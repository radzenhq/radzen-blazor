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
    private string delimiter = string.Empty;
    private Block? closed;
    private bool tight;

    private readonly bool verbatimBlocks;

    private MarkdownWriter(string source, bool verbatimBlocks)
    {
        this.source = source;
        this.verbatimBlocks = verbatimBlocks;
    }

    public static string Write(Document document, string source) => Serialize(document, source, verbatimBlocks: false);

    public static string Preserve(Document document, string source) => Serialize(document, source, verbatimBlocks: true);

    private static string Serialize(Document document, string source, bool verbatimBlocks)
    {
        var writer = new MarkdownWriter(source, verbatimBlocks);
        document.Accept(writer);
        return writer.output.ToString();
    }

    private bool AtBlank => output.Length == 0 || output[^1] == '\n';

    private static bool IsPristine(Block block) => block.Pristine && block switch
    {
        BlockContainer container => container.Children.All(IsPristine),
        Table table => table.Rows.All(row => row.Cells.All(cell => cell.Pristine)),
        _ => true
    };

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
        Emit(text);
    }

    private void EnsureNewLine()
    {
        if (!AtBlank)
        {
            output.Append('\n');
        }
    }

    private void CloseBlock(Block block)
    {
        closed = block;
        closedContent = block is Paragraph { Children.Count: 0 } ? closedContent : block;
    }

    private Block? closedContent;

    private void WrapBlock(string delim, string? firstDelim, Block block, Action content)
    {
        var previous = delimiter;
        FlushClose(tight ? 1 : 2);
        Emit(firstDelim ?? delim);
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

    private void Emit(string text)
    {
        if (delimiter.Length > 0 && AtBlank)
        {
            output.Append(delimiter);
        }

        output.Append(text);
    }

    private bool definitionsFollow;

    public void VisitDocument(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);
        Block? previous = null;
        Block? content = null;
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

            var verbatim = verbatimBlocks && IsPristine(block) && block.SourceEnd > block.SourceStart && !(block is IndentedCodeBlock && content is List);
            var previousVerbatim = previous != null && verbatimBlocks && IsPristine(previous) && previous.SourceEnd > previous.SourceStart && previous.SourceEnd <= block.SourceStart;

            var gapCopied = verbatim && previousVerbatim && source.AsSpan(previous!.SourceEnd, block.SourceStart - previous.SourceEnd).IsWhiteSpace() && BlankLines.Placeholders(source, previous.SourceEnd, block.SourceStart, true, true).Count == pending.Count;

            if (gapCopied)
            {
                closed = null;
                output.Append(source, previous!.SourceEnd, block.SourceStart - previous.SourceEnd);
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

                if (block is IndentedCodeBlock && !gapCopied)
                {
                    output.Append("    ");
                }

                output.Append(source, block.SourceStart, block.SourceEnd - block.SourceStart);
                CloseBlock(block);
            }
            else
            {
                block.Accept(this);
            }

            previous = block;
            content = block is Paragraph { Children.Count: 0 } ? content : block;
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

            if (ownLine)
            {
                output.Append('\n');
            }

            CloseBlock(paragraph);
            return;
        }

        FlushClose(LazySeparator());
        Emit(string.Empty);
        RenderInlines(InlineNormalizer.Normalize(paragraph.Children, lineStart: true), "\n", lineStart: true, blockEnd: true);
        CloseBlock(paragraph);
    }

    public void VisitHeading(Heading heading)
    {
        ArgumentNullException.ThrowIfNull(heading);

        var inlines = InlineNormalizer.Normalize(heading.Children, lineStart: true);

        if (heading is SetExtHeading && inlines.Count > 0 && !(tight && heading.Parent is { } parent && parent.IndexOf(heading) is > 0 and var at && parent.Children[at - 1] is Paragraph))
        {
            FlushClose(LazySeparator());
            Emit(string.Empty);
            RenderInlines(inlines, "\n", lineStart: true, blockEnd: true);
            var underline = ((SetExtHeading)heading).Underline ?? string.Empty;
            EnsureNewLine();
            Write(Regex.IsMatch(underline, "^[=-]+$") ? underline : new string(heading.Level == 1 ? '=' : '-', 3));
            CloseBlock(heading);
            return;
        }

        var prefix = new string('#', heading.Level) + (heading.Children.Count > 0 ? " " : string.Empty);
        Write(prefix);
        var enclosing = constructs;
        constructs |= Construct.HeadingAtx;
        RenderInlines(InlineNormalizer.Normalize(heading.Children, lineStart: false), "\n", blockEnd: true);
        constructs = enclosing;
        CloseBlock(heading);
    }

    public void VisitThematicBreak(ThematicBreak thematicBreak)
    {
        ArgumentNullException.ThrowIfNull(thematicBreak);
        var line = thematicBreak.Line ?? string.Empty;
        line = line.Length > 0 && !line.Contains('\n', StringComparison.Ordinal) ? line : "---";
        // CommonMark 0.31.2, 4.1 Thematic breaks and 4.3 Setext headings: the line must not read as the list marker or as a heading underline
        line = thematicBreak.Parent is ListItem item && (item.Children[0] == thematicBreak || item.Children[item.IndexOf(thematicBreak) - 1] is Paragraph) && line[0] is '-' or '*' ? "___" : line;
        Write(line);
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

            var marker = Marker(list, item, index);
            var padding = Math.Max(list.Padding, marker.Length + 1);
            var firstDelim = marker + new string(' ', padding - marker.Length);
            var continuation = new string(' ', padding);
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

    private static string Marker(List list, ListItem item, int index)
    {
        if (list is OrderedList ordered)
        {
            var number = item.Pristine && list.Pristine && item.Number is { } original ? original : ordered.Start + index;
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

        if (item.Children.Count > 0 && item.Checked != null && item.Children[0] is not (Paragraph or Heading))
        {
            output.Append('\n');
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
        var closed = fencedCodeBlock.Closed || HasFollowing(fencedCodeBlock) || definitionsFollow;

        if (fencedCodeBlock.Value.Length > 0)
        {
            WriteLines(fencedCodeBlock.Value, keepTrailing: !closed, leadingNewline: true, continuation: delimiter);
        }

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

    private void WriteLines(string value, bool keepTrailing, bool leadingNewline, string continuation)
    {
        var lines = value.Split('\n');
        var count = lines.Length > 1 && lines[^1].Length == 0 && !keepTrailing ? lines.Length - 1 : lines.Length;

        for (var index = 0; index < count; index++)
        {
            if (leadingNewline || index > 0)
            {
                output.Append('\n');
            }

            if (lines[index].Length > 0)
            {
                output.Append(AtBlank ? delimiter : continuation).Append(lines[index]);
            }
            else if (AtBlank && (!keepTrailing || index < count - 1))
            {
                output.Append(delimiter.TrimEnd());
            }
        }
    }

    public void VisitIndentedCodeBlock(IndentedCodeBlock codeBlock)
    {
        ArgumentNullException.ThrowIfNull(codeBlock);
        var fence = closedContent is List or ListItem ? new string('`', Math.Max(3, LongestRun(codeBlock.Value, '`') + 1)) : null;
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

        WriteLines(codeBlock.Value, keepTrailing: false, leadingNewline: false, continuation: "    ");

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

        WriteLines(string.Join('\n', lines), keepTrailing: true, leadingNewline: false, continuation: string.Empty);

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
            RenderInlines(InlineNormalizer.Normalize(cell.Children, lineStart: false), " ");
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
        RenderInlines(InlineNormalizer.Normalize(cell.Children, lineStart: false), " ");
    }

    private int inlineDepth;

    private void RenderInlines(IReadOnlyList<Inline> inlines, string after = "\n", bool lineStart = false, bool blockEnd = false)
    {
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

        for (var index = 0; index < trailing; index++)
        {
            next = index + 1 < trailing ? inlines[index + 1] : null;
            following = next != null ? InlineNormalizer.FirstChar(next, after[0]).ToString() : after;
            atBlockStart = inlineDepth == 1 && index == 0 && lineStart;
            inlines[index].Accept(this);
        }

        inlineDepth--;
        constructs = enclosing;
    }

    private string following = "\n";

    private Inline? next;

    private bool atBlockStart;

    private int lineStartAt;

    private bool contentAtLineStart;

    private string Before() => (contentAtLineStart ? "\n" : string.Empty) + output.ToString(lineStartAt, output.Length - lineStartAt);

    public void VisitText(Text text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var value = text.Value;
        var leading = Unflanked(value) ? Reference(value[0]) : string.Empty;
        value = leading.Length > 0 ? value[1..] : value;
        var trailing = Unflanking(value) ? Reference(value[^1]) : string.Empty;
        value = trailing.Length > 0 ? value[..^1] : value;
        Emit(leading + Safe(value, Before() + leading, trailing.Length > 0 ? trailing : following).Replace("\n", "&#10;", StringComparison.Ordinal) + trailing);
        atInlineLineStart = false;
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
        new("#", After: @"[ \t]*(?:[\r\n]|$)", In: Construct.HeadingAtx),
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

    private string Safe(string value, string before, string after)
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

        for (var index = start; index < end; index++)
        {
            var ch = whole[index];

            if (ch.IsEscapable() && ((infos.TryGetValue(index, out var info) && !Skipped(index, info)) || (ch == '\\' && index + 1 < whole.Length && whole[index + 1].IsEscapable())))
            {
                escaped.Append('\\');
            }

            escaped.Append(ch);
        }

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

    private void RenderMark(InlineContainer inline, string marker)
    {
        if (InlineNormalizer.Blank(inline.Children))
        {
            return;
        }

        Emit(marker);
        RenderInlines(inline.Children, marker);
        output.Append(marker);
        closerEnd = output.Length;
    }

    private int closerEnd = -1;

    // CommonMark 0.31.2, 6.2 Emphasis and strong emphasis: a delimiter run between punctuation and a letter is not flanking, so the letter is written as a character reference
    private bool Unflanked(string value)
    {
        if (closerEnd != output.Length || value.Length == 0 || !char.IsLetterOrDigit(value[0]))
        {
            return false;
        }

        var marker = output[^1];
        var run = output.Length;

        while (run > 0 && output[run - 1] == marker)
        {
            run--;
        }

        return marker == '_' || (run > 0 && (output[run - 1] == '\\' ? run < output.Length - 1 : output[run - 1].IsPunctuation()));
    }

    private bool Unflanking(string value) => next is Emphasis or Strong or Strikethrough && value.Length > 0 && char.IsLetterOrDigit(value[^1]) && (InlineNormalizer.Marker(next) == '_' || (Opening((InlineContainer)next) is var inner && !char.IsWhiteSpace(inner) && inner.IsPunctuation()));

    private static char Opening(InlineContainer mark)
    {
        while (mark.Children.Count > 0 && mark.Children[0] is Emphasis or Strong or Strikethrough)
        {
            var child = (InlineContainer)mark.Children[0];

            if (InlineNormalizer.Marker(child) != InlineNormalizer.Marker(mark))
            {
                return InlineNormalizer.Marker(child);
            }

            mark = child;
        }

        return mark.Children.Count > 0 ? InlineNormalizer.FirstChar(mark.Children[0], ' ') : ' ';
    }

    private static string Reference(char ch) => "&#" + ((int)ch).ToString(CultureInfo.InvariantCulture) + ";";

    public void VisitCode(Code code)
    {
        ArgumentNullException.ThrowIfNull(code);
        var value = code.Value.Replace('\n', ' ');
        // GFM 0.29-gfm, 4.10 Tables: a pipe inside a code span still splits the cell unless it is escaped
        value = (constructs & Construct.TableCell) != 0 ? value.Replace("|", "\\|", StringComparison.Ordinal) : value;
        var ticks = new string('`', Math.Max(code.Ticks ?? 1, LongestRun(value, '`') + 1));
        var padded = value.Length > 0 && (value[0] == '`' || value[^1] == '`' || (value[0] == ' ' && value[^1] == ' ' && value.Trim().Length > 0));
        Emit(ticks);

        if (padded)
        {
            output.Append(' ');
        }

        output.Append(value);

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
        atInlineLineStart = false;
        var enclosing = constructs;

        if (suffix != null)
        {
            Emit(image ? "![" : "[");
            constructs |= Construct.Label;
            RenderInlines(inline.Children, "]");
            constructs = enclosing;
            atInlineLineStart = false;
            output.Append(suffix);
            return;
        }

        if (inline is Link { Autolink: true } && inline.Children is [Text only] && (only.Value == destination || "mailto:" + only.Value == destination))
        {
            Emit("<");
            output.Append(only.Value);
            output.Append('>');
            return;
        }

        Emit(image ? "![" : "[");
        constructs |= Construct.Label;
        RenderInlines(inline.Children, "](");
        constructs = enclosing;
        atInlineLineStart = false;
        output.Append("](");
        var target = (destination ?? string.Empty).Replace("\r", "%0D", StringComparison.Ordinal).Replace("\n", "%0A", StringComparison.Ordinal);

        if ((target.Length == 0 && !string.IsNullOrEmpty(title)) || Regex.IsMatch(target, @"[\x00-\x20\x7F]"))
        {
            constructs = enclosing | Construct.DestinationLiteral;
            output.Append('<').Append(Safe(target, "<", ">")).Append('>');
        }
        else
        {
            constructs = enclosing | Construct.DestinationRaw;
            output.Append(Safe(target, "(", string.IsNullOrEmpty(title) ? ")" : " "));
        }

        if (!string.IsNullOrEmpty(title))
        {
            constructs = enclosing | Construct.Title;
            output.Append(" \"").Append(Safe(title, "\"", "\"")).Append('"');
        }

        constructs = enclosing;
        output.Append(')');
    }

    // CommonMark 0.31.2, 4.6 HTML blocks: only start conditions 1 to 6 may interrupt a paragraph
    // CommonMark 0.31.2, 4.6 HTML blocks: kind 7 cannot interrupt a paragraph, so it only starts a block on the first line
    private bool StartsHtmlBlock(string? value)
    {
        if (value == null)
        {
            return false;
        }

        var line = value.IndexOf('\n', StringComparison.Ordinal) is var end && end >= 0 ? value[..end] : value;
        return Enumerable.Range(1, atBlockStart && end < 0 && LineEnds(next) ? 7 : 6).Any(type => BlockParser.HtmlBlockOpenRegex[type].IsMatch(line));
    }

    private static bool LineEnds(Inline? next) => next switch
    {
        null or LineBreak or SoftLineBreak => true,
        Text text => string.IsNullOrWhiteSpace(text.Value),
        _ => false
    };

    public void VisitHtmlInline(HtmlInline html)
    {
        ArgumentNullException.ThrowIfNull(html);
        var value = html.Value ?? string.Empty;
        var escaped = atInlineLineStart && StartsHtmlBlock(value);
        // CommonMark 0.31.2, 4.4 Indented code blocks: an indented line cannot interrupt a paragraph
        Emit((escaped ? "\\" : string.Empty) + value.Replace("\n", "\n    ", StringComparison.Ordinal));
        atInlineLineStart = false;
    }

    public void VisitLineBreak(LineBreak lineBreak)
    {
        ArgumentNullException.ThrowIfNull(lineBreak);
        Emit(lineBreak.Backslash == true || atInlineLineStart ? "\\" : "  ");
        output.Append('\n');
        Emit(string.Empty);
        atInlineLineStart = true;
        contentAtLineStart = true;
        lineStartAt = output.Length;
    }

    public void VisitSoftLineBreak(SoftLineBreak softLineBreak)
    {
        ArgumentNullException.ThrowIfNull(softLineBreak);
        output.Append('\n');
        Emit(string.Empty);
        atInlineLineStart = true;
        contentAtLineStart = true;
        lineStartAt = output.Length;
    }
}
