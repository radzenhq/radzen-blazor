using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Radzen.Documents.Markdown;

namespace Radzen.Blazor;

internal sealed class MarkdownEditorEngine
{
    private sealed class Edit
    {
        public int Start { get; set; }
        public string Removed { get; set; } = string.Empty;
        public string Inserted { get; set; } = string.Empty;
        public (int Start, int End) Before { get; set; }
        public (int Start, int End) After { get; set; }
        public string? Key { get; set; }
        public bool Design { get; set; }
    }

    private sealed record Cursor(Block Block, IBlockInlineContainer? Content, int Offset);

    private sealed record Host(Block Block, IBlockInlineContainer? Content, int Start, int Length)
    {
        public int End => Start + Length;
    }

    private readonly List<Edit> undo = [];
    private readonly List<Edit> redo = [];
    private (int Start, int End) pending;
    private (int Start, int End) pendingSource;
    private (int Position, List<Mark> Marks)? stored;
    private string source;
    private Document? document;

    public MarkdownEditorEngine(string? text)
    {
        Text = Normalize(text);
        source = Text;
    }

    public string Text { get; private set; }

    public bool RenderHtml { get; set; } = true;

    public bool CanUndo => undo.Count > 0;

    public bool CanRedo => redo.Count > 0;

    public static string Normalize(string? text) => (text ?? string.Empty).Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\r", "\n", StringComparison.Ordinal);

    public void Reset(string? text)
    {
        Text = Normalize(text);
        document = null;
        stored = null;
        undo.Clear();
        redo.Clear();
    }

    private Document Model
    {
        get
        {
            if (document == null)
            {
                source = Text;
                document = Fresh();
            }

            return document;
        }
    }

    private Document Fresh() => BlankLines.Inflate(MarkdownParser.Parse(Text), Text);

    public MarkdownEditorUpdate Apply(int start, int end, string inserted, (int Start, int End) after, string? key = null, bool merge = false, (int Start, int End)? before = null)
    {
        document = null;
        stored = null;
        ApplyEdit(start, end, inserted, after, key, merge, before, design: false);
        return Render(after.Start, after.End);
    }

    private void ApplyEdit(int start, int end, string inserted, (int Start, int End) after, string? key, bool merge, (int Start, int End)? before, bool design)
    {
        start = Math.Clamp(start, 0, Text.Length);
        end = Math.Clamp(end, start, Text.Length);

        if (Text[start..end] == inserted)
        {
            return;
        }

        var edit = new Edit
        {
            Start = start,
            Removed = Text[start..end],
            Inserted = inserted,
            Before = before ?? (start, end),
            After = after,
            Key = key,
            Design = design
        };

        Text = Text[..start] + inserted + Text[end..];
        redo.Clear();

        var last = undo.Count > 0 ? undo[^1] : null;

        if (merge && key != null && last?.Key == key && last.Design == design && Merge(last, edit))
        {
            last.After = after;
        }
        else
        {
            undo.Add(edit);

            if (undo.Count > 200)
            {
                undo.RemoveAt(0);
            }
        }
    }

    private static bool Merge(Edit last, Edit next)
    {
        if (next.Removed.Length == 0 && last.Start + last.Inserted.Length == next.Start)
        {
            last.Inserted += next.Inserted;
            return true;
        }

        if (next.Inserted.Length == 0 && last.Inserted.Length == 0 && next.Start + next.Removed.Length == last.Start)
        {
            last.Start = next.Start;
            last.Removed = next.Removed + last.Removed;
            return true;
        }

        if (next.Inserted.Length == 0 && last.Inserted.Length == 0 && next.Start == last.Start)
        {
            last.Removed += next.Removed;
            return true;
        }

        return false;
    }

    public MarkdownEditorUpdate? Undo()
    {
        if (undo.Count == 0)
        {
            return null;
        }

        var edit = undo[^1];
        undo.RemoveAt(undo.Count - 1);
        Text = Text[..edit.Start] + edit.Removed + Text[(edit.Start + edit.Inserted.Length)..];
        document = null;
        stored = null;
        redo.Add(edit);
        var (start, end) = Convert(edit.Before, edit.Design);

        return Render(start, end, includeText: true);
    }

    public MarkdownEditorUpdate? Redo()
    {
        if (redo.Count == 0)
        {
            return null;
        }

        var edit = redo[^1];
        redo.RemoveAt(redo.Count - 1);
        Text = Text[..edit.Start] + edit.Inserted + Text[(edit.Start + edit.Removed.Length)..];
        document = null;
        stored = null;
        undo.Add(edit);
        var (start, end) = Convert(edit.After, edit.Design);

        return Render(start, end, includeText: true);
    }

    private (int Start, int End) Convert((int Start, int End) selection, bool design)
    {
        if (design == RenderHtml)
        {
            return selection;
        }

        return design ? (ToSource(selection.Start), ToSource(selection.End)) : (ToPosition(selection.Start), ToPosition(selection.End));
    }

    public MarkdownEditorUpdate Render(int selectionStart, int selectionEnd, bool includeText = false)
    {
        if (!RenderHtml)
        {
            document = null;
        }

        var current = Model;
        var hosts = Hosts(current);
        var limit = RenderHtml ? Size(hosts) : Text.Length;
        selectionStart = Math.Clamp(selectionStart, 0, limit);
        selectionEnd = Math.Clamp(selectionEnd, selectionStart, limit);
        var rendered = RenderHtml ? HtmlVisitor.Render(current) : null;
        var segments = rendered == null ? null : new int[rendered.Segments.Count * 3];

        for (var index = 0; rendered != null && index < rendered.Segments.Count; index++)
        {
            var segment = rendered.Segments[index];
            segments![index * 3] = segment.Start;
            segments[index * 3 + 1] = segment.End;
            segments[index * 3 + 2] = segment.Length;
        }

        var state = RenderHtml ? State(current, hosts, selectionStart, selectionEnd) : State(current, hosts, ToPosition(current, hosts, selectionStart), ToPosition(current, hosts, selectionEnd));

        return new MarkdownEditorUpdate
        {
            Html = rendered?.Html,
            Segments = segments,
            Text = includeText ? Text : null,
            SelectionStart = selectionStart,
            SelectionEnd = selectionEnd,
            State = state
        };
    }

    public MarkdownEditorToolState State(int selectionStart, int selectionEnd)
    {
        var current = Model;
        var hosts = Hosts(current);

        if (!RenderHtml)
        {
            selectionStart = ToPosition(current, hosts, selectionStart);
            selectionEnd = ToPosition(current, hosts, selectionEnd);
        }

        (selectionStart, selectionEnd) = Clamp(hosts, selectionStart, selectionEnd);
        stored = stored is { } kept && selectionStart == selectionEnd && selectionStart == kept.Position ? stored : null;
        return State(current, hosts, selectionStart, selectionEnd);
    }

    internal int Length => Size(Hosts(Model));

    internal INode HostAt(int position)
    {
        var host = HostAt(Hosts(Model), position);
        return host.Content ?? (INode)host.Block;
    }

    private MarkdownEditorToolState State(Document document, List<Host> hosts, int start, int end)
    {
        var formats = new List<string>();
        var cursor = Resolve(hosts, start);
        var chain = Chain(cursor.Block);
        var innermostItem = chain.OfType<ListItem>().LastOrDefault();

        foreach (var block in chain)
        {
            switch (block)
            {
                case BlockQuote:
                    formats.Add(MarkdownEditorCommands.Quote);
                    break;
                case ListItem item when item == innermostItem:
                    formats.Add(ListKindOf(item));
                    break;
                case FencedCodeBlock or IndentedCodeBlock:
                    formats.Add(MarkdownEditorCommands.CodeBlock);
                    break;
            }
        }

        if (stored is { } typing && typing.Position == start && start == end)
        {
            formats.AddRange(typing.Marks.Select(FormatOf).OfType<string>());
        }
        else if (cursor.Content != null)
        {
            formats.AddRange(MarksAt(Runs(cursor.Content), cursor.Offset).Select(FormatOf).OfType<string>());
        }

        string? kind = null;
        var first = true;

        foreach (var candidate in Leaves(hosts, start, end).Select(Kind))
        {
            kind = first ? candidate : kind == candidate ? kind : string.Empty;
            first = false;
        }

        if (kind == null && cursor.Block is Paragraph or ListItem)
        {
            kind = "p";
        }

        var state = new MarkdownEditorToolState
        {
            Formats = formats.Distinct().ToArray(),
            Block = kind,
            CanUndo = CanUndo,
            CanRedo = CanRedo
        };

        if (cursor.Block is Table table && cursor.Content is TableCell cell)
        {
            var row = table.Rows.First(candidate => candidate.Cells.Contains(cell));
            state.TableRow = IndexOf(table.Rows, row);
            state.TableColumn = IndexOf(row.Cells, cell);
            state.TableRows = table.Rows.Count;
            state.TableColumns = table.Rows[0].Cells.Count;
            state.TableAlignment = table.Rows[0].Cells[Math.Min(state.TableColumn, table.Rows[0].Cells.Count - 1)].Alignment.ToString().ToLowerInvariant();
        }

        return state;
    }

    private static List<Mark> MarksAt(IReadOnlyList<Run> runs, int offset)
    {
        var (index, within) = InlineContent.Locate(runs, offset);

        if (index < runs.Count && runs[index].Atom == null && (within > 0 || index == 0 || runs[index - 1].Atom != null))
        {
            return runs[index].Marks;
        }

        return index > 0 && within == 0 && runs[index - 1].Atom == null ? runs[index - 1].Marks : [];
    }

    private static string? FormatOf(Mark mark) => mark.Kind switch
    {
        MarkKind.Strong => MarkdownEditorCommands.Bold,
        MarkKind.Emphasis => MarkdownEditorCommands.Italic,
        MarkKind.Strikethrough => MarkdownEditorCommands.Strikethrough,
        MarkKind.Code => MarkdownEditorCommands.Code,
        MarkKind.Link => MarkdownEditorCommands.Link,
        _ => null
    };

    private static string ListKindOf(ListItem item) => item.Checked != null ? MarkdownEditorCommands.TaskList : item.Parent is OrderedList ? MarkdownEditorCommands.OrderedList : MarkdownEditorCommands.UnorderedList;

    private static IEnumerable<Leaf> Leaves(List<Host> hosts, int start, int end) => hosts.Where(host => host.Start <= end && host.End >= start && host.Content is Paragraph or Heading).Select(host => (Leaf)host.Content!);

    private static string? Kind(Leaf leaf) => leaf switch
    {
        Heading heading => "h" + heading.Level.ToString(CultureInfo.InvariantCulture),
        Paragraph => "p",
        _ => null
    };

    private static List<Host> Hosts(Document document)
    {
        var hosts = new List<Host>();
        var position = 0;
        Collect(document);
        return hosts;

        void Add(Block block, IBlockInlineContainer? content, int length)
        {
            hosts.Add(new Host(block, content, position, length));
            position += length + 1;
        }

        void Collect(BlockContainer container)
        {
            if (container.Children.Count == 0 && container is not Document)
            {
                Add(container, null, 0);
                return;
            }

            foreach (var block in container.Children)
            {
                switch (block)
                {
                    case BlockContainer nested:
                        Collect(nested);
                        break;
                    case Table table:
                        foreach (var cell in table.Rows.SelectMany(row => row.Cells))
                        {
                            Add(table, cell, InlineLength(cell));
                        }

                        break;
                    case FencedCodeBlock or IndentedCodeBlock or HtmlBlock:
                        Add(block, null, HtmlVisitor.CodeLength((Leaf)block));
                        break;
                    case Leaf leaf:
                        Add(leaf, leaf, InlineLength(leaf));
                        break;
                    default:
                        Add(block, null, 0);
                        break;
                }
            }
        }
    }

    private static int InlineLength(IBlockInlineContainer content) => InlineContent.Length(InlineContent.Flatten(content.Children));

    private static int Size(List<Host> hosts) => hosts.Count == 0 ? 0 : hosts[^1].End;

    private static (int Start, int End) Clamp(List<Host> hosts, int start, int end)
    {
        var size = Size(hosts);
        start = Math.Clamp(start, 0, size);
        end = Math.Clamp(end, start, size);
        return (start, end);
    }

    private (int Start, int End) Incoming(Document document, List<Host> hosts, int start, int end)
    {
        pendingSource = (start, end);
        stored = stored is { } kept && start == end && start == kept.Position ? stored : null;

        if (!RenderHtml)
        {
            start = ToPosition(document, hosts, start);
            end = ToPosition(document, hosts, end);
        }

        return Clamp(hosts, start, end);
    }

    private static Host HostAt(List<Host> hosts, int position)
    {
        foreach (var host in hosts)
        {
            if (position <= host.End)
            {
                return host;
            }
        }

        return hosts[^1];
    }

    private static Cursor Resolve(List<Host> hosts, int position)
    {
        var host = HostAt(hosts, position);
        return new Cursor(host.Block, host.Content, Math.Clamp(position - host.Start, 0, host.Length));
    }

    private static int Position(List<Host> hosts, INode node, int offset)
    {
        Host? host = null;

        foreach (var candidate in hosts)
        {
            if (candidate.Content == node || (candidate.Content == null && candidate.Block == node))
            {
                host = candidate;
                break;
            }
        }

        host ??= node is Block block ? hosts.FirstOrDefault(candidate => candidate.Block == block || IsAncestor(block, candidate.Block)) : null;

        return host == null ? -1 : host.Start + Math.Clamp(offset, 0, host.Length);
    }

    private static (int Start, int End) Extent(List<Host> hosts, Block block)
    {
        var start = int.MaxValue;
        var end = -1;

        foreach (var host in hosts)
        {
            if (host.Block == block || IsAncestor(block, host.Block))
            {
                start = Math.Min(start, host.Start);
                end = Math.Max(end, host.End);
            }
        }

        return end < 0 ? (-1, -1) : (start, end);
    }

    private static List<Block> Chain(Block block)
    {
        var chain = new List<Block>();

        for (Block? current = block; current is not null and not Document; current = current.Parent)
        {
            chain.Insert(0, current);
        }

        return chain;
    }

    private static IEnumerable<Block> Blocks(BlockContainer container)
    {
        foreach (var block in container.Children)
        {
            yield return block;

            if (block is BlockContainer nested)
            {
                foreach (var inner in Blocks(nested))
                {
                    yield return inner;
                }
            }
        }
    }

    internal int ToSource(int position, bool preferNext = false) => ToSource(Fresh(), Text, position, preferNext);

    private static int ToSource(Document parsed, string text, int position, bool preferNext)
    {
        var hosts = Hosts(parsed);
        var host = HostAt(hosts, Math.Clamp(position, 0, Size(hosts)));
        var offset = Math.Clamp(position - host.Start, 0, host.Length);
        var result = host switch
        {
            { Content: { } content } => ContentSource(text, content, offset, preferNext),
            { Block: FencedCodeBlock or IndentedCodeBlock or HtmlBlock } => ((Leaf)host.Block).Content.ToSource(offset),
            { Block: ThematicBreak } => host.Block.SourceStart,
            _ => LineEnd(text, host.Block.SourceEnd)
        };

        return Math.Clamp(result, 0, text.Length);
    }

    private static int LineEnd(string text, int offset)
    {
        while (offset < text.Length && text[offset] is ' ' or '\t')
        {
            offset++;
        }

        return offset;
    }

    private static List<(int Position, Run Run, Inline Node)> Spans(IBlockInlineContainer content)
    {
        var spans = new List<(int Position, Run Run, Inline Node)>();
        var position = 0;

        foreach (var run in InlineContent.Flatten(content.Children))
        {
            if ((run.Atom ?? run.Origin) is { } node)
            {
                spans.Add((position, run, node));
            }

            position += run.Length;
        }

        return spans;
    }

    private static int EmptySource(IBlockInlineContainer content) => content is TableCell { Content.Segments.Count: > 0 } cell ? cell.Content.Segments[0].SourceStart : content is Block block ? block.SourceEnd : 0;

    private static int ContentSource(string text, IBlockInlineContainer content, int offset, bool preferNext)
    {
        var spans = Spans(content);

        if (spans.Count == 0)
        {
            return EmptySource(content);
        }

        for (var index = 0; index < spans.Count; index++)
        {
            var (position, run, node) = spans[index];
            var end = position + run.Length;

            if (offset < end || (offset == end && (!preferNext || index == spans.Count - 1)))
            {
                var within = Math.Max(0, offset - position);

                return node switch
                {
                    _ when run.Atom != null => within > 0 ? node.SourceEnd : node.SourceStart,
                    Text value => value.SourceStart + SourceOffsetWithin(text, value, within),
                    Code code => CodeContentStart(text, code) + within,
                    _ => node.SourceStart
                };
            }
        }

        return spans[^1].Node.SourceEnd;
    }

    internal int ToPosition(int offset)
    {
        var parsed = Fresh();
        return ToPosition(parsed, Hosts(parsed), offset);
    }

    private int ToPosition(Document parsed, List<Host> hosts, int offset)
    {
        offset = Math.Clamp(offset, 0, Text.Length);
        Host? previous = null;
        var previousEnd = 0;

        foreach (var host in hosts)
        {
            var (start, end) = SourceRange(host);

            if (offset < start)
            {
                var inside = Chain(host.Block).Any(block => block.SourceStart <= offset && offset <= block.SourceEnd);
                return !inside && previous != null && offset - previousEnd <= start - offset ? previous.End : host.Start;
            }

            if (offset <= end)
            {
                return host.Start + Within(host, offset);
            }

            previous = host;
            previousEnd = end;
        }

        return Size(hosts);
    }

    private static (int Start, int End) SourceRange(Host host)
    {
        switch (host)
        {
            case { Block: FencedCodeBlock or IndentedCodeBlock or HtmlBlock }:
                var code = (Leaf)host.Block;
                return (code.Content.ToSource(0), code.Content.ToSourceEnd(host.Length));
            case { Content: { } content }:
                var spans = Spans(content);
                var empty = EmptySource(content);
                return spans.Count > 0 ? (spans[0].Node.SourceStart, spans[^1].Node.SourceEnd) : (empty, empty);
            case { Block: ThematicBreak }:
                return (host.Block.SourceStart, host.Block.SourceStart);
            default:
                return (host.Block.SourceEnd, host.Block.SourceEnd);
        }
    }

    private int Within(Host host, int offset)
    {
        if (host.Content == null)
        {
            return host.Block is Leaf code ? Math.Clamp(code.Content.ToContent(offset), 0, host.Length) : 0;
        }

        foreach (var (position, run, node) in Spans(host.Content))
        {
            if (offset < node.SourceStart)
            {
                return position;
            }

            if (offset <= node.SourceEnd)
            {
                return node switch
                {
                    _ when run.Atom != null => offset < node.SourceEnd ? position : position + 1,
                    Text text => position + ValueOffsetWithin(Text, text, offset - text.SourceStart),
                    Code code => position + Math.Clamp(offset - CodeContentStart(Text, code), 0, code.Value.Length),
                    _ => position
                };
            }
        }

        return host.Length;
    }

    private static int SourceOffsetWithin(string source, Text text, int valueOffset)
    {
        var slice = text.SourceEnd > text.SourceStart && text.SourceEnd <= source.Length ? source[text.SourceStart..text.SourceEnd] : text.Value;
        var i = 0;
        var j = 0;

        while (j < valueOffset && i < slice.Length)
        {
            i += slice[i] == '\\' && i + 1 < slice.Length && j < text.Value.Length && slice[i + 1] == text.Value[j] ? 2 : 1;
            j++;
        }

        return i;
    }

    private static int ValueOffsetWithin(string source, Text text, int sourceOffset)
    {
        var slice = text.SourceEnd > text.SourceStart && text.SourceEnd <= source.Length ? source[text.SourceStart..text.SourceEnd] : text.Value;
        var i = 0;
        var j = 0;

        while (i < sourceOffset && i < slice.Length)
        {
            i += slice[i] == '\\' && i + 1 < slice.Length && j < text.Value.Length && slice[i + 1] == text.Value[j] ? 2 : 1;
            j++;
        }

        return Math.Min(j, text.Value.Length);
    }

    private static int CodeContentStart(string text, Code code)
    {
        var start = code.SourceStart;

        while (start < text.Length && text[start] == '`')
        {
            start++;
        }

        return start < text.Length && text[start] == ' ' && code.Value.Length > 0 && (code.Value[0] != ' ' || code.Value.Trim().Length == 0) ? start + 1 : start;
    }

    private static void NormalizeLists(BlockContainer container)
    {
        var index = 0;

        while (index < container.Children.Count)
        {
            var block = container.Children[index];

            if (block is BlockContainer nested)
            {
                NormalizeLists(nested);
            }

            if (block is List current)
            {
                foreach (var step in new[] { -1, 1 })
                {
                    var neighbour = index + step;
                    var gapTouched = false;

                    while (neighbour >= 0 && neighbour < container.Children.Count && container.Children[neighbour] is Paragraph { Children.Count: 0 } gap)
                    {
                        gapTouched |= !gap.Pristine;
                        neighbour += step;
                    }

                    if (neighbour < 0 || neighbour >= container.Children.Count || container.Children[neighbour] is not List other || other.GetType() != current.GetType() || (step > 0 && (!other.Pristine || current.Pristine)) || (step < 0 && current.Pristine && !gapTouched))
                    {
                        continue;
                    }

                    if (current is OrderedList ordered && other is OrderedList otherOrdered && ordered.Delimiter == otherOrdered.Delimiter)
                    {
                        ordered.Delimiter = ordered.Delimiter == "." ? ")" : ".";
                        Touch(current);
                    }
                    else if (current is UnorderedList && other.Marker == current.Marker)
                    {
                        current.Marker = current.Marker == '-' ? '*' : '-';
                        Touch(current);
                    }
                }
            }

            index++;
        }
    }

    private MarkdownEditorUpdate CommitAtEnd(Document document, Block block) => block switch
    {
        FencedCodeBlock or IndentedCodeBlock or HtmlBlock => Commit(document, block, HtmlVisitor.CodeLength((Leaf)block)),
        IBlockInlineContainer content => Commit(document, content, InlineLength(content)),
        BlockContainer container when LastLeaf(container) is { } leaf => CommitAtEnd(document, leaf),
        _ => Commit(document, block, 0)
    };

    private MarkdownEditorUpdate Commit(Document document, INode caretNode, int caretOffset, INode? endNode = null, int endOffset = 0, string? key = null, bool merge = false)
    {
        NormalizeLists(document);
        BlankLines.Pad(document, int.MaxValue, caretNode);
        var replacement = MarkdownWriter.Preserve(document, source);
        var hosts = Hosts(document);
        var start = Position(hosts, caretNode, caretOffset);
        start = start < 0 ? Math.Clamp(pending.Start, 0, Size(hosts)) : start;
        var end = endNode == null ? start : Position(hosts, endNode, endOffset);
        end = end < 0 ? start : end;
        var selection = (Math.Min(start, end), Math.Max(start, end));
        var before = pending;

        if (!RenderHtml)
        {
            var parsed = BlankLines.Inflate(MarkdownParser.Parse(replacement), replacement);
            selection = (ToSource(parsed, replacement, selection.Item1, preferNext: selection.Item1 < selection.Item2), ToSource(parsed, replacement, selection.Item2, preferNext: false));
            before = pendingSource;
        }

        if (replacement != Text)
        {
            var prefix = 0;

            while (prefix < Text.Length && prefix < replacement.Length && Text[prefix] == replacement[prefix])
            {
                prefix++;
            }

            var suffix = 0;

            while (suffix < Text.Length - prefix && suffix < replacement.Length - prefix && Text[Text.Length - 1 - suffix] == replacement[replacement.Length - 1 - suffix])
            {
                suffix++;
            }

            ApplyEdit(prefix, Text.Length - suffix, replacement[prefix..(replacement.Length - suffix)], selection, key, merge, before, design: RenderHtml);
        }

        this.document = document;
        stored = null;
        return Render(selection.Item1, selection.Item2);
    }

    private static int IndexOf<T>(IReadOnlyList<T> items, T item) where T : class
    {
        for (var index = 0; index < items.Count; index++)
        {
            if (ReferenceEquals(items[index], item))
            {
                return index;
            }
        }

        return -1;
    }

    private static void Touch(Block? block)
    {
        while (block != null)
        {
            block.Pristine = false;
            block = block.Parent;
        }
    }

    private static void SetRuns(IBlockInlineContainer content, IReadOnlyList<Run> runs)
    {
        var inlines = InlineContent.Rebuild(runs);

        switch (content)
        {
            case Leaf leaf:
                leaf.ReplaceInlines(inlines);
                Touch(leaf);
                break;
            case TableCell cell:
                cell.ReplaceInlines(inlines);
                cell.Pristine = false;
                break;
        }
    }

    private static List<Run> Runs(IBlockInlineContainer content) => InlineContent.Flatten(content.Children);

    public MarkdownEditorUpdate InsertText(int start, int end, string text, bool literal, string? key = null, bool merge = false, bool paragraphs = false, bool selection = false, bool preferRight = false)
    {
        var document = Model;
        var hosts = Hosts(document);
        (start, end) = Incoming(document, hosts, start, end);
        pending = selection ? (start, end) : (end, end);

        if (!literal)
        {
            return InsertMarkdown(document, hosts, start, end, text) ?? Render(start, start);
        }

        var marks = stored?.Marks;
        var cursor = start < end ? DeleteRange(document, hosts, start, end) : Resolve(hosts, start);

        if (cursor == null)
        {
            return Render(start, start);
        }

        return InsertAt(document, cursor, text, paragraphs, key, merge, preferRight, marks) ?? Render(start, start);
    }

    private MarkdownEditorUpdate? InsertAt(Document document, Cursor cursor, string text, bool paragraphs, string? key, bool merge, bool preferRight, List<Mark>? marks)
    {
        text = Normalize(text);

        if (cursor.Block is FencedCodeBlock or IndentedCodeBlock or HtmlBlock)
        {
            var code = (Leaf)cursor.Block;
            code.Value = code.Value[..cursor.Offset] + text + code.Value[cursor.Offset..];
            Touch(code);
            return Commit(document, code, cursor.Offset + text.Length, key: key, merge: merge);
        }

        if (cursor.Block is ListItem item && cursor.Content == null)
        {
            var paragraph = new Paragraph();
            item.Insert(0, paragraph);
            Touch(item);
            cursor = new Cursor(paragraph, paragraph, 0);
        }

        if (cursor.Content == null)
        {
            return null;
        }

        if (cursor.Content is Paragraph { Virtual: true } virtualParagraph)
        {
            virtualParagraph.Virtual = false;
        }

        var lines = paragraphs && cursor.Content is Leaf ? text.Split('\n').Where(line => line.Length > 0).ToArray() : [text.Replace('\n', ' ')];

        if (lines.Length == 0)
        {
            return null;
        }

        SetRuns(cursor.Content, InlineContent.Insert(Runs(cursor.Content), cursor.Offset, lines[0], marks, preferRight));
        INode caretContent = cursor.Content;
        var caretOffset = cursor.Offset + lines[0].Length;

        if (lines.Length > 1 && cursor.Content is Leaf leaf)
        {
            var tail = SplitLeaf(leaf, caretOffset);
            Block previous = leaf;

            for (var index = 1; index < lines.Length; index++)
            {
                var paragraph = new Paragraph();
                paragraph.Add(new Text(lines[index]));
                InsertAfter(previous, paragraph);
                previous = paragraph;
            }

            caretOffset = lines[^1].Length;

            if (tail.Children.Count > 0 && previous is Paragraph last)
            {
                SetRuns(last, InlineContent.Flatten(last.Children).Concat(InlineContent.Flatten(tail.Children)).ToList());
            }

            RemoveBlock(tail);
            caretContent = previous;
        }

        return Commit(document, caretContent, caretOffset, key: key, merge: merge);
    }

    private Cursor? DeleteRange(Document document, List<Host> hosts, int start, int end)
    {
        var first = Resolve(hosts, start);
        var last = Resolve(hosts, end);
        var firstExtent = Extent(hosts, first.Block);
        var lastExtent = Extent(hosts, last.Block);
        var firstCovered = firstExtent.Start >= start && firstExtent.End <= end && first.Block is not Paragraph { Children.Count: 0 };
        var lastCovered = lastExtent.Start >= start && lastExtent.End <= end && last.Block is not Paragraph { Children.Count: 0 };

        if (first.Block == last.Block && first.Content == last.Content && !(BlankLines.IsSealed(first.Block) && firstCovered))
        {
            if (first.Block is FencedCodeBlock or IndentedCodeBlock or HtmlBlock)
            {
                var code = (Leaf)first.Block;
                code.Value = code.Value[..first.Offset] + code.Value[last.Offset..];
                Touch(code);
                return first;
            }

            if (first.Content != null)
            {
                SetRuns(first.Content, InlineContent.Delete(Runs(first.Content), first.Offset, last.Offset));
            }

            return first;
        }

        if ((BlankLines.IsSealed(first.Block) && !firstCovered) || (BlankLines.IsSealed(last.Block) && !lastCovered))
        {
            return null;
        }

        var covered = Blocks(document).Where(block => block != first.Block && block != last.Block && Extent(hosts, block) is var extent && extent.Start >= firstExtent.Start && extent.End <= lastExtent.End && !IsAncestor(block, first.Block) && !IsAncestor(block, last.Block)).ToList();

        if (covered.Any(block => BlankLines.IsSealed(block) && Extent(hosts, block) is var extent && (extent.Start < start || extent.End > end)))
        {
            return null;
        }

        var target = firstCovered ? null : first.Content as Leaf;

        if (target != null)
        {
            SetRuns(target, InlineContent.Split(Runs(target), first.Offset, out _));
        }

        if (last.Content is Leaf lastLeaf && !lastCovered)
        {
            InlineContent.Split(Runs(lastLeaf), last.Offset, out var tail);

            if (target != null)
            {
                SetRuns(target, InlineContent.Flatten(target.Children).Concat(tail).ToList());
                var dissolved = lastLeaf.Parent is ListItem item && item != target.Parent && !IsAncestor(item, target) && item.IndexOf(lastLeaf) == 0 ? item : null;
                var rest = dissolved?.Children.Skip(1).ToList() ?? [];
                RemoveBlock(lastLeaf);

                if (dissolved != null && rest.Count > 0)
                {
                    Splice(target.Parent, target.Parent.IndexOf(target) + 1, rest);
                    Prune(dissolved);
                }
            }
            else
            {
                SetRuns(lastLeaf, tail);
            }
        }
        else if (lastCovered)
        {
            RemoveBlock(last.Block);
        }

        if (firstCovered && first.Block != last.Block)
        {
            RemoveBlock(first.Block);
        }

        foreach (var block in covered.Where(block => block is not BlockContainer).ToList())
        {
            RemoveBlock(block);
        }

        foreach (var container in covered.OfType<BlockContainer>().Where(container => container.Parent != null).ToList())
        {
            if (Blocks(container).All(inner => inner is BlockContainer))
            {
                RemoveBlock(container);
            }
        }

        if (target is Heading { Children.Count: 0 } emptyHeading && Attached(document, emptyHeading))
        {
            var paragraph = new Paragraph();
            emptyHeading.Parent.Replace(emptyHeading, paragraph);
            Touch(paragraph);
            return new Cursor(paragraph, paragraph, 0);
        }

        if (target != null && Attached(document, target))
        {
            return new Cursor(target, target, first.Offset);
        }

        if (last.Content is Leaf remaining && Attached(document, remaining))
        {
            return new Cursor(remaining, remaining, 0);
        }

        if (hosts.LastOrDefault(host => host.End < start && host.Content is Paragraph or Heading && !BlankLines.IsSealed(host.Block) && Attached(document, host.Block)) is { } before)
        {
            var leaf = (Leaf)before.Content!;
            return new Cursor(leaf, leaf, InlineLength(leaf));
        }

        var replacement = new Paragraph();

        if (document.Children.Count == 0)
        {
            document.Add(replacement);
        }
        else
        {
            var anchor = document.Children.FirstOrDefault(block => Extent(hosts, block).Start >= start) ?? document.Children[^1];
            InsertBefore(anchor, replacement);
        }

        Touch(replacement);
        return new Cursor(replacement, replacement, 0);
    }

    private static bool Attached(Document document, Block block)
    {
        Block current = block;

        while (current.Parent is { } parent)
        {
            if (!parent.Children.Contains(current))
            {
                return false;
            }

            current = parent;
        }

        return current == document;
    }

    private static bool IsAncestor(Block ancestor, Block block)
    {
        var current = block.Parent as Block;

        while (current != null)
        {
            if (current == ancestor)
            {
                return true;
            }

            current = current.Parent as Block;
        }

        return false;
    }

    private static void RemoveBlock(Block block)
    {
        var parent = block.Parent;

        if (parent == null)
        {
            return;
        }

        Touch(parent);
        parent.Remove(block);

        if (parent.Children.Count == 0 && parent is ListItem or BlockQuote or List)
        {
            RemoveBlock(parent);
        }
    }

    private static void InsertAfter(Block anchor, Block block)
    {
        var parent = anchor.Parent;
        parent.Insert(parent.IndexOf(anchor) + 1, block);
        Touch(block);
    }

    private static void InsertBefore(Block anchor, Block block)
    {
        var parent = anchor.Parent;
        parent.Insert(parent.IndexOf(anchor), block);
        Touch(block);
    }

    private static Leaf SplitLeaf(Leaf leaf, int offset)
    {
        var runs = InlineContent.Flatten(leaf.Children);
        var head = InlineContent.Split(runs, offset, out var tail);
        SetRuns(leaf, Trim(head, start: false));
        Leaf second = leaf is Heading heading ? new AtxHeading { Level = heading.Level } : new Paragraph();
        second.ReplaceInlines(InlineContent.Rebuild(Trim(tail, start: true)));
        InsertAfter(leaf, second);
        return second;
    }

    private static List<Run> Trim(List<Run> runs, bool start)
    {
        while (runs.Count > 0)
        {
            var index = start ? 0 : runs.Count - 1;
            var run = runs[index];
            var trimmed = run.Atom != null ? run.Text : start ? run.Text.TrimStart() : run.Text.TrimEnd();

            if (trimmed.Length == run.Length)
            {
                break;
            }

            if (trimmed.Length == 0)
            {
                runs.RemoveAt(index);
                continue;
            }

            runs[index] = new Run(trimmed, run.Marks, null);
            break;
        }

        return runs;
    }

    public MarkdownEditorUpdate? Delete(int start, int end, bool forward = false, string? key = null, bool merge = false, bool selection = false)
    {
        var document = Model;
        var hosts = Hosts(document);
        (start, end) = Incoming(document, hosts, start, end);
        pending = selection ? (start, end) : forward ? (start, start) : (end, end);

        if (selection || end - start > 1)
        {
            var cursor = DeleteRange(document, hosts, start, end);
            return cursor == null ? null : Commit(document, cursor.Content ?? (INode)cursor.Block, cursor.Offset);
        }

        var caret = Resolve(hosts, forward ? start : end);

        if (caret.Block is ThematicBreak rule)
        {
            return RemoveAndMove(document, rule, forward);
        }

        if (caret.Block is FencedCodeBlock or IndentedCodeBlock or HtmlBlock)
        {
            var code = (Leaf)caret.Block;

            if (forward ? caret.Offset >= HtmlVisitor.CodeLength(code) : caret.Offset == 0)
            {
                return code.Value.Trim().Length > 0 ? null : RemoveAndMove(document, code, forward);
            }

            var from = forward ? caret.Offset : caret.Offset - 1;
            code.Value = code.Value[..from] + code.Value[(from + 1)..];
            Touch(code);
            return Commit(document, code, from, key: key, merge: merge);
        }

        if (caret.Content == null)
        {
            return caret.Block is ListItem item && !forward ? LiftItem(document, item, null, 0) : null;
        }

        var runs = Runs(caret.Content);
        var length = InlineContent.Length(runs);

        if (!forward && caret.Offset == 0)
        {
            return JoinBackward(document, caret.Block, caret.Content);
        }

        if (forward && caret.Offset >= length)
        {
            return JoinForward(document, caret.Block, caret.Content);
        }

        var text = string.Concat(runs.Select(run => run.Text));
        var deleteStart = forward ? caret.Offset : caret.Offset - 1;
        var deleteEnd = deleteStart + 1;

        if (deleteStart > 0 && char.IsLowSurrogate(text[deleteStart]) && char.IsHighSurrogate(text[deleteStart - 1]))
        {
            deleteStart--;
        }

        if (deleteEnd < text.Length && char.IsLowSurrogate(text[deleteEnd]) && char.IsHighSurrogate(text[deleteEnd - 1]))
        {
            deleteEnd++;
        }

        SetRuns(caret.Content, InlineContent.Delete(runs, deleteStart, deleteEnd));
        return Commit(document, caret.Content, deleteStart, key: key, merge: merge);
    }

    private MarkdownEditorUpdate? Select(Document document, Block block)
    {
        var (start, end) = Extent(Hosts(document), block);
        return start < end ? Render(start, end) : null;
    }

    private MarkdownEditorUpdate RemoveAndMove(Document document, Block block, bool forward)
    {
        var parent = block.Parent;
        var index = parent.IndexOf(block);
        var previous = parent.Children.Take(index).LastOrDefault(candidate => candidate is not Paragraph { Virtual: true });
        var next = parent.Children.Skip(index + 1).FirstOrDefault(candidate => candidate is not Paragraph { Virtual: true });
        RemoveBlock(block);

        foreach (var target in forward ? new[] { next, previous } : [previous, next])
        {
            if (target != null && !BlankLines.IsSealed(target))
            {
                return target == previous ? CommitAtEnd(document, target) : Commit(document, target, 0);
            }
        }

        var replacement = new Paragraph();
        parent.Insert(previous != null ? parent.IndexOf(previous) + 1 : 0, replacement);
        Touch(replacement);
        return Commit(document, replacement, 0);
    }

    private MarkdownEditorUpdate? JoinBackward(Document document, Block block, IBlockInlineContainer content)
    {
        if (content is TableCell cell && block is Table table)
        {
            var (row, rowIndex, _) = Locate(table, cell);

            if (rowIndex > 0 && row.Cells.All(candidate => candidate.Children.Count == 0))
            {
                table.RemoveRow(row);
                Touch(table);
                var target = table.Rows[rowIndex - 1].Cells[^1];
                return Commit(document, target, InlineLength(target));
            }

            return null;
        }

        if (block is not Leaf leaf)
        {
            return null;
        }

        var container = leaf.Parent;
        var index = container.IndexOf(leaf);
        var previous = index > 0 ? container.Children[index - 1] : null;

        if (previous is Paragraph { Virtual: true })
        {
            return null;
        }

        if (previous is ThematicBreak rule)
        {
            RemoveBlock(rule);
            return Commit(document, leaf, 0);
        }

        if (previous != null && BlankLines.IsSealed(previous))
        {
            return Select(document, previous) ?? RemoveAndMove(document, previous, forward: true);
        }

        if (previous is Paragraph { Children.Count: 0 } emptyParagraph)
        {
            RemoveBlock(emptyParagraph);
            return Commit(document, leaf, 0);
        }

        if (leaf is Heading heading && previous is not (Paragraph or Heading))
        {
            var paragraph = new Paragraph();
            paragraph.ReplaceInlines(heading.Children.ToList());
            container.Replace(heading, paragraph);
            Touch(paragraph);
            return Commit(document, paragraph, 0);
        }

        if (container is ListItem item && index == 0)
        {
            return LiftItem(document, item, leaf, 0);
        }

        if (container is BlockQuote quote && index == 0)
        {
            quote.Remove(leaf);
            InsertBefore(quote, leaf);
            Touch(quote);
            Prune(quote);
            return Commit(document, leaf, 0);
        }

        if (previous == null)
        {
            return null;
        }

        if (leaf.Children.Count == 0)
        {
            RemoveBlock(leaf);
            return CommitAtEnd(document, LastLeaf(previous) is Paragraph or Heading ? LastLeaf(previous)! : previous);
        }

        var previousLeaf = LastLeaf(previous);

        if (previousLeaf is not (Paragraph or Heading) || BlankLines.IsSealed(previous))
        {
            return null;
        }

        var next = index + 1 < container.Children.Count ? container.Children[index + 1] : null;
        var joinAt = JoinLeaves(previousLeaf, leaf);

        if (previous is List previousList && next is List nextList && previousList.GetType() == nextList.GetType())
        {
            MergeLists(previousList, nextList);
        }

        return Commit(document, previousLeaf, joinAt);
    }

    private static int JoinLeaves(Leaf target, Leaf source)
    {
        var runs = InlineContent.Flatten(target.Children);
        var joinAt = InlineContent.Length(runs);
        SetRuns(target, runs.Concat(Runs(source)).ToList());
        var container = source.Parent;
        container.Remove(source);
        Touch(container);
        Prune(container);
        return joinAt;
    }

    private static void Prune(BlockContainer container)
    {
        if (container.Children.Count == 0 && container is ListItem or BlockQuote or List)
        {
            RemoveBlock(container);
        }
    }

    private static void MergeLists(List into, List from)
    {
        foreach (var moved in from.Children.ToList())
        {
            from.Remove(moved);
            into.Add(moved);
            Touch(moved);
        }

        into.Tight = into.Tight && from.Tight;
        from.Parent.Remove(from);
        Touch(into);
    }

    private static Leaf? LastLeaf(Block block)
    {
        while (true)
        {
            if (block is Leaf leaf)
            {
                return leaf;
            }

            if (block is BlockContainer { LastChild: { } last })
            {
                block = last;
                continue;
            }

            return null;
        }
    }

    private static Leaf? FirstLeaf(Block block)
    {
        while (true)
        {
            if (block is Leaf leaf)
            {
                return leaf;
            }

            if (block is BlockContainer { FirstChild: { } first })
            {
                block = first;
                continue;
            }

            return null;
        }
    }

    private MarkdownEditorUpdate? JoinForward(Document document, Block block, IBlockInlineContainer content)
    {
        if (content is TableCell || block is not Leaf leaf)
        {
            return null;
        }

        var next = NextBlock(leaf);

        if (leaf.Children.Count == 0 && leaf is Paragraph { Virtual: false })
        {
            var container = leaf.Parent;
            var index = container.IndexOf(leaf);
            var previousBlock = index > 0 ? container.Children[index - 1] : null;
            RemoveBlock(leaf);

            if (previousBlock != null)
            {
                return CommitAtEnd(document, LastLeaf(previousBlock) is Paragraph or Heading ? LastLeaf(previousBlock)! : previousBlock);
            }

            return Commit(document, next ?? (document.Children.Count > 0 ? document.Children[0] : leaf), 0);
        }

        if (next == null || next is Paragraph { Virtual: true })
        {
            return null;
        }

        if (next is ThematicBreak rule)
        {
            RemoveBlock(rule);
            return CommitAtEnd(document, leaf);
        }

        if (BlankLines.IsSealed(next))
        {
            return Select(document, next) ?? RemoveAndMove(document, next, forward: false);
        }

        if (next is Paragraph { Children.Count: 0 } empty)
        {
            RemoveBlock(empty);
            return CommitAtEnd(document, leaf);
        }

        var nextLeaf = FirstLeaf(next);

        if (nextLeaf is not (Paragraph or Heading) || BlankLines.IsSealed(next))
        {
            return null;
        }

        return Commit(document, leaf, JoinLeaves(leaf, nextLeaf));
    }

    private static Block? NextBlock(Block block)
    {
        var current = block;

        while (current != null)
        {
            var parent = current.Parent;

            if (parent == null)
            {
                return null;
            }

            var index = parent.IndexOf(current);

            if (index + 1 < parent.Children.Count)
            {
                return parent.Children[index + 1];
            }

            current = parent;
        }

        return null;
    }

    private MarkdownEditorUpdate? LiftItem(Document document, ListItem item, Leaf? leaf, int caretOffset)
    {
        var list = (List)item.Parent;

        if (list.Parent is ListItem)
        {
            Outdent(item);
            var target = leaf ?? FirstLeaf(item);
            return target != null ? Commit(document, target, caretOffset) : Commit(document, item, 0);
        }

        var lifted = LiftItems(list, [item]);
        var caretTarget = leaf ?? lifted[0] as Leaf;
        return caretTarget != null ? Commit(document, caretTarget, caretOffset) : Commit(document, lifted[0], 0);
    }

    private static void Outdent(ListItem item)
    {
        var list = (List)item.Parent;
        var outerItem = (ListItem)list.Parent;
        var outerList = (List)outerItem.Parent;
        var index = list.IndexOf(item);
        var following = list.Children.Skip(index + 1).ToList();
        list.Remove(item);

        foreach (var follower in following)
        {
            list.Remove(follower);
        }

        if (following.Count > 0)
        {
            item.Add(CloneList(list, following));
        }

        if (list.Children.Count == 0)
        {
            outerItem.Remove(list);
        }

        outerList.Insert(outerList.IndexOf(outerItem) + 1, item);
        Touch(item);
        Touch(outerList);
    }

    public MarkdownEditorUpdate? InsertParagraph(int start, int end) => InsertBreak(start, end, paragraph: true);

    public MarkdownEditorUpdate? InsertLineBreak(int start, int end) => InsertBreak(start, end, paragraph: false);

    private MarkdownEditorUpdate? InsertBreak(int start, int end, bool paragraph)
    {
        var document = Model;
        var hosts = Hosts(document);
        (start, end) = Incoming(document, hosts, start, end);
        pending = (start, start);
        var cursor = start < end ? DeleteRange(document, hosts, start, end) : Resolve(hosts, start);

        if (cursor == null)
        {
            return null;
        }

        if (cursor.Block is FencedCodeBlock or IndentedCodeBlock or HtmlBlock)
        {
            var code = (Leaf)cursor.Block;

            if (paragraph && cursor.Block is FencedCodeBlock && cursor.Offset >= code.Value.Length - 1 && code.Value.EndsWith("\n\n", StringComparison.Ordinal))
            {
                code.Value = code.Value[..^1];
                Touch(code);
                var exit = new Paragraph();
                InsertAfter(code, exit);
                return Commit(document, exit, 0);
            }

            code.Value = code.Value[..cursor.Offset] + "\n" + code.Value[cursor.Offset..];
            Touch(code);
            return Commit(document, code, cursor.Offset + 1);
        }

        if (cursor.Block is Table table)
        {
            return paragraph && cursor.Content is TableCell cell ? InsertRow(document, table, cell, sameColumn: true) : null;
        }

        if (cursor.Block is ListItem emptyItem && cursor.Content == null)
        {
            return paragraph ? LiftItem(document, emptyItem, null, 0) : null;
        }

        if (cursor.Content is not Leaf leaf)
        {
            return null;
        }

        var leafRuns = Runs(leaf);
        var length = InlineContent.Length(leafRuns);

        if (!paragraph)
        {
            var head = InlineContent.Split(leafRuns, cursor.Offset, out var tail);
            var marks = head.Count > 0 ? head[^1].Marks : tail.Count > 0 ? tail[0].Marks : [];
            SetRuns(leaf, [.. head, new Run(new LineBreak { Backslash = false }, marks), .. tail]);
            return Commit(document, leaf, cursor.Offset + 1);
        }

        var container = leaf.Parent;

        if (leaf is Paragraph { Virtual: true } virtualParagraph)
        {
            virtualParagraph.Virtual = false;
            Touch(virtualParagraph);
            return Commit(document, virtualParagraph, 0);
        }

        if (container is ListItem item && length == 0 && item.Children.Count == 1)
        {
            return LiftItem(document, item, leaf, 0);
        }

        if (container is BlockQuote quote && length == 0 && container.LastChild == leaf)
        {
            quote.Remove(leaf);
            InsertAfter(quote, leaf);
            Touch(quote);
            Prune(quote);
            return Commit(document, leaf, 0);
        }

        if (leaf is Heading heading)
        {
            if (cursor.Offset == 0)
            {
                InsertBefore(heading, new Paragraph());
                Touch(heading);
                return Commit(document, heading, 0);
            }

            if (cursor.Offset >= length)
            {
                var after = new Paragraph();
                InsertAfter(heading, after);
                return Commit(document, after, 0);
            }

            return Commit(document, SplitLeaf(heading, cursor.Offset), 0);
        }

        if (container is ListItem listItem)
        {
            var next = new ListItem { Checked = listItem.Checked != null ? false : null };
            var second = SplitLeaf(leaf, cursor.Offset);
            var moved = listItem.Children.Skip(listItem.IndexOf(second)).ToList();

            if (moved.Count == 1 && second.Children.Count == 0)
            {
                listItem.Remove(second);
                moved.Clear();
            }

            foreach (var follower in moved)
            {
                listItem.Remove(follower);
                next.Add(follower);
            }

            listItem.Parent.Insert(listItem.Parent.IndexOf(listItem) + 1, next);
            Touch(next);
            Touch(listItem);
            return Commit(document, moved.Count > 0 ? second : next, 0);
        }

        return Commit(document, SplitLeaf(leaf, cursor.Offset), 0);
    }

    private static T Fill<T>(T row, int columns, TableRow? like) where T : TableRow
    {
        for (var index = 0; index < columns; index++)
        {
            row.Add(string.Empty, like != null && index < like.Cells.Count ? like.Cells[index].Alignment : TableCellAlignment.None);
            row.Cells[index].Pristine = false;
        }

        return row;
    }

    private static (TableRow Row, int RowIndex, int Column) Locate(Table table, TableCell cell)
    {
        var row = table.Rows.First(candidate => candidate.Cells.Contains(cell));
        return (row, IndexOf(table.Rows, row), IndexOf(row.Cells, cell));
    }

    private MarkdownEditorUpdate InsertRow(Document document, Table table, TableCell cell, bool sameColumn)
    {
        var (row, rowIndex, column) = Locate(table, cell);

        if (sameColumn && rowIndex == table.Rows.Count - 1 && rowIndex > 0 && row.Cells.All(candidate => candidate.Children.Count == 0))
        {
            table.RemoveRow(row);
            Touch(table);
            var exit = new Paragraph();
            InsertAfter(table, exit);
            return Commit(document, exit, 0);
        }

        var added = Fill(new TableRow(), table.Rows[0].Cells.Count, table.Rows[0]);
        table.InsertRow(rowIndex + 1, added);
        Touch(table);
        return Commit(document, added.Cells[sameColumn ? Math.Min(column, added.Cells.Count - 1) : 0], 0);
    }

    public MarkdownEditorUpdate? AppendRow(int caret)
    {
        var document = Model;
        var hosts = Hosts(document);
        (caret, _) = Incoming(document, hosts, caret, caret);
        var cursor = Resolve(hosts, caret);
        pending = (caret, caret);
        return cursor.Block is Table table && cursor.Content is TableCell cell ? InsertRow(document, table, cell, sameColumn: false) : null;
    }

    public MarkdownEditorUpdate? ToggleCheck(int position)
    {
        var document = Model;
        var hosts = Hosts(document);
        (position, _) = Incoming(document, hosts, position, position);
        pending = (position, position);
        var cursor = Resolve(hosts, position);
        var item = Chain(cursor.Block).OfType<ListItem>().LastOrDefault(candidate => candidate.Checked != null);

        if (item == null)
        {
            return null;
        }

        item.Checked = !item.Checked;
        Touch(item);
        return Commit(document, cursor.Content ?? (INode)cursor.Block, cursor.Offset);
    }

    public MarkdownEditorUpdate? Indent(int start, int end, bool outdent)
    {
        var document = Model;
        var hosts = Hosts(document);
        (start, end) = Incoming(document, hosts, start, end);
        pending = (start, end);
        var cursor = Resolve(hosts, start);
        var item = cursor.Block is ListItem direct ? direct : cursor.Block.Parent as ListItem;

        if (item == null)
        {
            return null;
        }

        var list = (List)item.Parent;
        var caretNode = (INode?)cursor.Content ?? item;

        if (outdent)
        {
            if (list.Parent is not ListItem)
            {
                return null;
            }

            Outdent(item);
            return Commit(document, caretNode, cursor.Offset);
        }

        var previous = list.Children.Take(list.IndexOf(item)).OfType<ListItem>().LastOrDefault();

        if (previous == null)
        {
            return null;
        }

        list.Remove(item);
        Touch(list);

        if (previous.LastChild is List nested)
        {
            nested.Add(item);
            Touch(nested);
        }
        else
        {
            previous.Add(CloneList(list, [item]));
        }

        Touch(item);
        return Commit(document, caretNode, cursor.Offset);
    }

    public MarkdownEditorUpdate? Command(string name, int start, int end, string? value, string? label)
    {
        var document = Model;
        var hosts = Hosts(document);
        (start, end) = Incoming(document, hosts, start, end);
        pending = (start, end);

        switch (name)
        {
            case MarkdownEditorCommands.Bold:
            case MarkdownEditorCommands.Italic:
            case MarkdownEditorCommands.Strikethrough:
            case MarkdownEditorCommands.Code:
                return ToggleMark(document, hosts, name, start, end);
            case MarkdownEditorCommands.FormatBlock:
                return FormatBlock(document, hosts, start, end, value);
            case MarkdownEditorCommands.Quote:
                return ToggleQuote(document, hosts, start, end);
            case MarkdownEditorCommands.UnorderedList:
            case MarkdownEditorCommands.OrderedList:
            case MarkdownEditorCommands.TaskList:
                return ToggleList(document, hosts, name, start, end);
            case MarkdownEditorCommands.CodeBlock:
                return ToggleCodeBlock(document, hosts, start);
            case MarkdownEditorCommands.HorizontalRule:
                return InsertRule(document, hosts, end);
            case MarkdownEditorCommands.Link:
            case MarkdownEditorCommands.Image:
                return InsertLink(document, hosts, name == MarkdownEditorCommands.Image, start, end, value, label);
            case MarkdownEditorCommands.InsertText:
                return InsertMarkdown(document, hosts, start, end, value ?? string.Empty);
            case MarkdownEditorCommands.InsertTable:
                return InsertTable(document, hosts, start, value);
            case MarkdownEditorCommands.TableRowBefore:
            case MarkdownEditorCommands.TableRowAfter:
            case MarkdownEditorCommands.TableColumnBefore:
            case MarkdownEditorCommands.TableColumnAfter:
            case MarkdownEditorCommands.TableDeleteRow:
            case MarkdownEditorCommands.TableDeleteColumn:
            case MarkdownEditorCommands.TableDelete:
            case MarkdownEditorCommands.TableAlign:
                return TableCommand(document, hosts, name, start, value);
            default:
                return null;
        }
    }

    private static MarkKind MarkKindOf(string name) => name switch
    {
        MarkdownEditorCommands.Bold => MarkKind.Strong,
        MarkdownEditorCommands.Italic => MarkKind.Emphasis,
        MarkdownEditorCommands.Strikethrough => MarkKind.Strikethrough,
        _ => MarkKind.Code
    };

    private MarkdownEditorUpdate? ToggleMark(Document document, List<Host> hosts, string name, int start, int end)
    {
        var kind = MarkKindOf(name);
        var first = Resolve(hosts, start);
        var last = Resolve(hosts, end);

        if (first.Content == null || first.Block is FencedCodeBlock or IndentedCodeBlock or HtmlBlock)
        {
            return null;
        }

        if (start == end && RenderHtml)
        {
            var current = stored is { } typing ? typing.Marks : MarksAt(Runs(first.Content), first.Offset);
            stored = (start, current.Any(mark => mark.Kind == kind) ? current.Where(mark => mark.Kind != kind).ToList() : [.. current, new Mark(kind)]);
            return Render(start, start);
        }

        if (start == end)
        {
            var runs = Runs(first.Content);
            var (wordStart, wordEnd) = Extent(runs, first.Offset, kind);

            if (wordStart == wordEnd)
            {
                return null;
            }

            SetRuns(first.Content, InlineContent.ToggleMark(runs, wordStart, wordEnd, new Mark(kind)));
            return Commit(document, first.Content, first.Offset);
        }

        var targets = Targets(hosts, start, end, first, last);

        if (targets.Count == 0)
        {
            return null;
        }

        var remove = targets.All(target => InlineContent.AllHave(InlineContent.Flatten(target.Content.Children), target.Start, target.End, kind));
        var mark = new Mark(kind);

        foreach (var (content, from, to) in targets)
        {
            var runs = Runs(content);
            var toggled = InlineContent.ToggleMark(runs, from, to, mark);

            if (InlineContent.AllHave(toggled, from, to, kind) == remove)
            {
                toggled = InlineContent.ToggleMark(toggled, from, to, mark);
            }

            SetRuns(content, toggled);
        }

        return Commit(document, first.Content, first.Offset, last.Content, last.Offset);
    }

    private static List<(IBlockInlineContainer Content, int Start, int End)> Targets(List<Host> hosts, int start, int end, Cursor first, Cursor last)
    {
        var targets = new List<(IBlockInlineContainer Content, int Start, int End)>();

        if (first.Content == last.Content)
        {
            targets.Add((first.Content!, Math.Min(first.Offset, last.Offset), Math.Max(first.Offset, last.Offset)));
            return targets;
        }

        foreach (var leaf in Leaves(hosts, start, end))
        {
            var from = leaf == first.Content ? first.Offset : 0;
            var to = leaf == last.Content ? last.Offset : InlineLength(leaf);

            if (from < to)
            {
                targets.Add((leaf, from, to));
            }
        }

        return targets;
    }

    private static (int Start, int End) Extent(IReadOnlyList<Run> runs, int offset, MarkKind kind)
    {
        var (index, _) = InlineContent.Locate(runs, offset);

        if (index < runs.Count && runs[index].HasMark(kind) && runs[index].Atom == null)
        {
            var position = 0;

            for (var i = 0; i < index; i++)
            {
                position += runs[i].Length;
            }

            var from = position;
            var to = position + runs[index].Length;

            for (var i = index - 1; i >= 0 && runs[i].HasMark(kind); i--)
            {
                from -= runs[i].Length;
            }

            for (var i = index + 1; i < runs.Count && runs[i].HasMark(kind); i++)
            {
                to += runs[i].Length;
            }

            return (from, to);
        }

        var text = string.Concat(runs.Select(run => run.Text));
        var start = Math.Min(offset, text.Length);
        var end = start;

        while (start > 0 && char.IsLetterOrDigit(text[start - 1]))
        {
            start--;
        }

        while (end < text.Length && char.IsLetterOrDigit(text[end]))
        {
            end++;
        }

        return (start, end);
    }

    private static List<Block> TopLevelBlocks(Document document, List<Host> hosts, int start, int end)
    {
        return document.Children.Where(block => block is not Paragraph { Virtual: true } && Extent(hosts, block) is var extent && extent.Start <= end && extent.End >= start).ToList();
    }

    private MarkdownEditorUpdate? FormatBlock(Document document, List<Host> hosts, int start, int end, string? value)
    {
        var level = value is ['h', >= '1' and <= '6'] ? value[1] - '0' : 0;
        var cursor = Resolve(hosts, start);
        var leaves = start == end ? (cursor.Content is Paragraph or Heading ? [(Leaf)cursor.Content] : new List<Leaf>()) : Leaves(hosts, start, end).ToList();
        var replacements = new Dictionary<Leaf, Leaf>(ReferenceEqualityComparer.Instance);

        foreach (var leaf in leaves)
        {
            if ((level == 0 && leaf is Paragraph) || (leaf is Heading existing && existing.Level == level))
            {
                continue;
            }

            Leaf replacement = level == 0 ? new Paragraph() : new AtxHeading { Level = level };
            replacement.ReplaceInlines(leaf.Children.ToList());
            leaf.Parent.Replace(leaf, replacement);
            Touch(replacement);

            if (level > 0 && replacement.Parent is ListItem item && item.Checked != null)
            {
                item.Checked = null;
            }

            replacements[leaf] = replacement;
        }

        if (replacements.Count == 0)
        {
            return null;
        }

        var caretLeaf = cursor.Content is Leaf original && replacements.TryGetValue(original, out var replaced) ? replaced : cursor.Content as Leaf ?? replacements.Values.First();

        if (start < end)
        {
            var last = Resolve(hosts, end);
            var endLeaf = last.Content is Leaf lastOriginal && replacements.TryGetValue(lastOriginal, out var lastReplaced) ? lastReplaced : last.Content as Leaf;
            return Commit(document, caretLeaf, cursor.Content is Leaf ? cursor.Offset : 0, endLeaf, endLeaf != null ? last.Offset : 0);
        }

        return Commit(document, caretLeaf, cursor.Content is Leaf ? cursor.Offset : 0);
    }

    private MarkdownEditorUpdate? ToggleQuote(Document document, List<Host> hosts, int start, int end)
    {
        var cursor = Resolve(hosts, start);
        var last = Resolve(hosts, end);
        var quote = Chain(cursor.Block).OfType<BlockQuote>().LastOrDefault();
        INode caretNode = cursor.Content ?? (INode)cursor.Block;

        if (quote != null && (start == end || Chain(last.Block).Contains(quote)))
        {
            Splice(quote.Parent, quote.Parent.IndexOf(quote), quote.Children.ToList());
            RemoveBlock(quote);
            return Commit(document, caretNode, cursor.Offset);
        }

        var blocks = start == end ? [TopLevel(cursor.Block)] : TopLevelBlocks(document, hosts, start, end);

        if (blocks.Count == 0)
        {
            return null;
        }

        var wrapper = new BlockQuote();
        var at = document.IndexOf(blocks[0]);

        foreach (var block in blocks)
        {
            document.Remove(block);
            wrapper.Add(block);
        }

        document.Insert(at, wrapper);
        Touch(wrapper);
        return Commit(document, caretNode, cursor.Offset, start < end ? last.Content : null, last.Offset);
    }

    private MarkdownEditorUpdate? ToggleList(Document document, List<Host> hosts, string name, int start, int end)
    {
        var cursor = Resolve(hosts, start);
        var selectionEnd = Resolve(hosts, end);
        var item = Chain(cursor.Block).OfType<ListItem>().LastOrDefault();
        INode caretNode = cursor.Content ?? (INode)cursor.Block;

        if (item != null)
        {
            if (ListKindOf(item) != name)
            {
                ConvertList((List)item.Parent, name);
                return Commit(document, caretNode, cursor.Offset, start < end ? selectionEnd.Content : null, selectionEnd.Offset);
            }

            if (start == end)
            {
                return LiftItem(document, item, cursor.Content as Leaf, cursor.Offset);
            }

            var list = (List)item.Parent;
            var selected = list.Children.OfType<ListItem>().Where(candidate => Extent(hosts, candidate) is var extent && extent.Start <= end && extent.End >= start).ToList();
            var caretLeaf = cursor.Content as Leaf;
            var endLeaf = selectionEnd.Content as Leaf;
            LiftItems(list, selected);
            return Commit(document, caretLeaf ?? caretNode, cursor.Offset, endLeaf, selectionEnd.Offset);
        }

        var blocks = (start == end ? [TopLevel(cursor.Block)] : TopLevelBlocks(document, hosts, start, end)).Where(block => block is not List).ToList();

        if (blocks.Count == 0)
        {
            return null;
        }

        List created = name == MarkdownEditorCommands.OrderedList ? new OrderedList { Start = 1, Delimiter = "." } : new UnorderedList();
        created.Marker = created is OrderedList ? '.' : '-';
        created.Padding = created is OrderedList ? 3 : 2;
        var at = document.IndexOf(blocks[0]);

        foreach (var block in blocks)
        {
            document.Remove(block);
            var listItem = new ListItem { Checked = name == MarkdownEditorCommands.TaskList ? false : null };

            if (block is Paragraph { Children.Count: 0 } emptyBlock)
            {
                emptyBlock.Virtual = false;
            }

            listItem.Add(block);
            created.Add(listItem);
        }

        document.Insert(at, created);
        Touch(created);
        return Commit(document, caretNode, cursor.Offset, start < end ? selectionEnd.Content : null, selectionEnd.Offset);
    }

    private static List<Block> LiftItems(List list, List<ListItem> items)
    {
        var parent = list.Parent;
        var index = parent.IndexOf(list);
        var before = list.Children.TakeWhile(child => !items.Contains(child)).ToList();
        var after = list.Children.SkipWhile(child => !items.Contains(child)).Skip(items.Count).ToList();
        var lifted = new List<Block>();
        parent.Remove(list);
        var insertAt = index;

        if (before.Count > 0)
        {
            parent.Insert(insertAt++, CloneList(list, before));
        }

        foreach (var item in items)
        {
            var children = item.Children.ToList();

            if (children.Count == 0)
            {
                children.Add(new Paragraph());
            }

            foreach (var child in children)
            {
                item.Remove(child);

                if (child is List nested)
                {
                    nested.MarkerOffset = 0;
                }

                parent.Insert(insertAt++, child);
                Touch(child);
                lifted.Add(child);
            }
        }

        if (after.Count > 0)
        {
            parent.Insert(insertAt, CloneList(list, after));
        }

        Touch(parent as Block);
        return lifted;
    }

    private static List CloneList(List list, IEnumerable<Block> children)
    {
        List clone = list is OrderedList ordered ? new OrderedList { Start = 1, Delimiter = ordered.Delimiter } : new UnorderedList();
        clone.Marker = list.Marker;
        clone.Padding = list.Padding;
        clone.Tight = list.Tight;

        foreach (var child in children.ToList())
        {
            child.Parent?.Remove(child);
            clone.Add(child);
        }

        Touch(clone);
        return clone;
    }

    private static void ConvertList(List list, string name)
    {
        var parent = list.Parent;
        var index = parent.IndexOf(list);
        List converted = name == MarkdownEditorCommands.OrderedList ? new OrderedList { Start = 1, Delimiter = "." } : new UnorderedList();
        converted.Marker = converted is OrderedList ? '.' : list is UnorderedList ? list.Marker : '-';
        converted.Padding = converted is OrderedList ? 3 : 2;
        converted.Tight = list.Tight;

        foreach (var child in list.Children.ToList())
        {
            list.Remove(child);

            if (child is ListItem item)
            {
                item.Checked = name == MarkdownEditorCommands.TaskList ? item.Checked ?? false : null;
            }

            converted.Add(child);
        }

        parent.Remove(list);
        parent.Insert(index, converted);
        Touch(converted);

        foreach (var child in converted.Children)
        {
            Touch(child);
        }

        if (index + 1 < parent.Children.Count && parent.Children[index + 1] is List following && following.GetType() == converted.GetType())
        {
            MergeLists(converted, following);
        }

        if (index > 0 && parent.Children[index - 1] is List preceding && preceding.GetType() == converted.GetType())
        {
            MergeLists(preceding, converted);
        }
    }

    private MarkdownEditorUpdate? ToggleCodeBlock(Document document, List<Host> hosts, int start)
    {
        var cursor = Resolve(hosts, start);

        if (cursor.Block is FencedCodeBlock or IndentedCodeBlock)
        {
            var code = (Leaf)cursor.Block;
            var parsed = MarkdownParser.Parse(code.Value.TrimEnd('\n'));
            var blocks = parsed.Children.Count > 0 ? parsed.Children.ToList() : [new Paragraph()];
            Splice(code.Parent, code.Parent.IndexOf(code), blocks);
            RemoveBlock(code);
            var target = blocks[0] as IBlockInlineContainer;
            return target != null ? Commit(document, target, Math.Min(cursor.Offset, InlineLength(target))) : Commit(document, blocks[0], 0);
        }

        if (cursor.Content is not Leaf leaf)
        {
            return null;
        }

        var text = leaf.Pristine && leaf.Children.Count > 0 && leaf.Children[^1].SourceEnd > leaf.Children[0].SourceStart && leaf.Children[^1].SourceEnd <= source.Length ? source[leaf.Children[0].SourceStart..leaf.Children[^1].SourceEnd] : InlineContent.PlainText(InlineContent.Flatten(leaf.Children));
        var fenced = new FencedCodeBlock { Value = text + "\n", Closed = true };
        leaf.Parent.Replace(leaf, fenced);
        Touch(fenced);
        return Commit(document, fenced, Math.Min(cursor.Offset, text.Length));
    }

    private static void Splice(BlockContainer parent, int index, IReadOnlyList<Block> blocks)
    {
        foreach (var block in blocks)
        {
            block.Parent?.Remove(block);
            parent.Insert(index++, block);
            Unpristine(block);
        }

        Touch(parent as Block);
    }

    private static void Unpristine(Block block)
    {
        block.Pristine = false;

        if (block is BlockContainer container)
        {
            foreach (var child in container.Children)
            {
                Unpristine(child);
            }
        }
    }

    private static Block TopLevel(Block block)
    {
        while (block.Parent is not Document && block.Parent != null)
        {
            block = block.Parent;
        }

        return block;
    }

    private MarkdownEditorUpdate InsertRule(Document document, List<Host> hosts, int caret)
    {
        var cursor = Resolve(hosts, caret);
        var rule = new ThematicBreak();

        if (cursor.Content is Paragraph { Children.Count: 0 } empty && empty.Parent is Document)
        {
            empty.Virtual = false;
            InsertBefore(empty, rule);
            return Commit(document, empty, 0);
        }

        if (cursor.Content is Paragraph inside && inside.Parent is Document && cursor.Offset > 0 && cursor.Offset < InlineLength(inside))
        {
            var second = SplitLeaf(inside, cursor.Offset);
            InsertBefore(second, rule);
            return Commit(document, second, 0);
        }

        var paragraph = new Paragraph();
        InsertAfter(TopLevel(cursor.Block), rule);
        InsertAfter(rule, paragraph);
        return Commit(document, paragraph, 0);
    }

    private MarkdownEditorUpdate? InsertLink(Document document, List<Host> hosts, bool image, int start, int end, string? value, string? label)
    {
        var cursor = Resolve(hosts, start);

        if (cursor.Content == null || string.IsNullOrEmpty(value))
        {
            return null;
        }

        var runs = Runs(cursor.Content);
        var last = Resolve(hosts, end);
        var to = start < end && last.Content == cursor.Content ? last.Offset : cursor.Offset;

        if (!image && start < end && last.Content != cursor.Content)
        {
            var mark = new Mark(MarkKind.Link, null, value);

            foreach (var (content, from, until) in Targets(hosts, start, end, cursor, last))
            {
                var linked = InlineContent.ToggleMark(Runs(content), from, until, mark);
                SetRuns(content, InlineContent.AllHave(linked, from, until, MarkKind.Link) ? linked : InlineContent.ToggleMark(linked, from, until, mark));
            }

            return Commit(document, cursor.Content, cursor.Offset, last.Content, last.Offset);
        }

        if (image)
        {
            var node = new Image { Destination = value };
            node.Add(new Text(label ?? string.Empty));
            var head = InlineContent.Split(InlineContent.Delete(runs, cursor.Offset, to), cursor.Offset, out var tail);
            SetRuns(cursor.Content, [.. head, new Run(node, []), .. tail]);
            return Commit(document, cursor.Content, cursor.Offset + 1);
        }

        var link = new Mark(MarkKind.Link, null, value);

        if (to > cursor.Offset)
        {
            var linked = InlineContent.ToggleMark(runs, cursor.Offset, to, link);

            if (!InlineContent.AllHave(linked, cursor.Offset, to, MarkKind.Link))
            {
                linked = InlineContent.ToggleMark(linked, cursor.Offset, to, link);
            }

            SetRuns(cursor.Content, linked);
            return Commit(document, cursor.Content, cursor.Offset, cursor.Content, to);
        }

        var text = string.IsNullOrEmpty(label) ? value : label;
        SetRuns(cursor.Content, InlineContent.Insert(runs, cursor.Offset, text, [link]));
        return Commit(document, cursor.Content, cursor.Offset + text.Length);
    }

    private MarkdownEditorUpdate? InsertMarkdown(Document document, List<Host> hosts, int start, int end, string value)
    {
        var cursor = start < end ? DeleteRange(document, hosts, start, end) : Resolve(hosts, start);

        if (cursor == null)
        {
            return null;
        }

        var fragment = MarkdownParser.Parse(Normalize(value));

        if (fragment.Children.Count == 1 && fragment.Children[0] is Paragraph inlineFragment && cursor.Content != null)
        {
            var runs = Runs(cursor.Content);
            var head = InlineContent.Split(runs, cursor.Offset, out var tail);
            var inserted = InlineContent.Flatten(inlineFragment.Children).Select(Detach).ToList();
            SetRuns(cursor.Content, head.Concat(inserted).Concat(tail).ToList());
            return Commit(document, cursor.Content, cursor.Offset + InlineContent.Length(inserted));
        }

        var blocks = fragment.Children.ToList();
        Splice(cursor.Block.Parent, cursor.Block.Parent.IndexOf(cursor.Block) + 1, blocks);
        return CommitAtEnd(document, blocks.Count > 0 ? blocks[^1] : cursor.Block);
    }

    private static Run Detach(Run run)
    {
        if (run.Atom != null)
        {
            return run;
        }

        return new Run(run.Text, run.Marks.Select(mark => new Mark(mark.Kind, null, mark.Destination, mark.Title)).ToList(), null);
    }

    private MarkdownEditorUpdate? InsertTable(Document document, List<Host> hosts, int start, string? value)
    {
        var parts = (value ?? "3x3").Split(['x', ',', ' '], StringSplitOptions.RemoveEmptyEntries);
        var rows = parts.Length > 0 && int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedRows) ? Math.Max(1, parsedRows) : 3;
        var columns = parts.Length > 1 && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedColumns) ? Math.Max(1, parsedColumns) : 3;
        var cursor = Resolve(hosts, start);
        var table = new Table();
        var header = Fill(new TableHeaderRow(), columns, null);
        table.InsertRow(0, header);

        for (var row = 1; row < rows; row++)
        {
            table.InsertRow(row, Fill(new TableRow(), columns, null));
        }

        var top = TopLevel(cursor.Block);

        if (top is Paragraph { Children.Count: 0 } empty)
        {
            empty.Parent.Replace(empty, table);
        }
        else
        {
            InsertAfter(top, table);
        }

        Touch(table);
        return Commit(document, header.Cells[0], 0);
    }

    private MarkdownEditorUpdate? TableCommand(Document document, List<Host> hosts, string name, int caret, string? value)
    {
        var cursor = Resolve(hosts, caret);

        if (cursor.Block is not Table table || cursor.Content is not TableCell cell)
        {
            return null;
        }

        var (row, rowIndex, column) = Locate(table, cell);
        var columns = table.Rows[0].Cells.Count;
        Touch(table);

        switch (name)
        {
            case MarkdownEditorCommands.TableRowBefore:
            case MarkdownEditorCommands.TableRowAfter:
                var at = name == MarkdownEditorCommands.TableRowBefore ? Math.Max(1, rowIndex) : rowIndex + 1;
                var added = Fill(new TableRow(), columns, table.Rows[0]);
                table.InsertRow(at, added);
                return Commit(document, added.Cells[Math.Min(column, columns - 1)], 0);
            case MarkdownEditorCommands.TableColumnBefore:
            case MarkdownEditorCommands.TableColumnAfter:
                var columnAt = name == MarkdownEditorCommands.TableColumnBefore ? column : column + 1;

                foreach (var candidate in table.Rows)
                {
                    candidate.Insert(Math.Min(columnAt, candidate.Cells.Count), new TableCell(string.Empty) { Pristine = false });
                }

                return Commit(document, row.Cells[columnAt], 0);
            case MarkdownEditorCommands.TableDeleteRow:
                if (rowIndex == 0)
                {
                    return null;
                }

                table.RemoveRow(row);
                var targetRow = table.Rows[Math.Min(rowIndex, table.Rows.Count - 1)];
                return Commit(document, targetRow.Cells[Math.Min(column, targetRow.Cells.Count - 1)], 0);
            case MarkdownEditorCommands.TableDeleteColumn:
                if (columns <= 1)
                {
                    return DeleteTable(document, table);
                }

                foreach (var candidate in table.Rows)
                {
                    if (column < candidate.Cells.Count)
                    {
                        candidate.RemoveAt(column);
                    }
                }

                return Commit(document, row.Cells[Math.Min(column, row.Cells.Count - 1)], 0);
            case MarkdownEditorCommands.TableDelete:
                return DeleteTable(document, table);
            case MarkdownEditorCommands.TableAlign:
                var alignment = value?.ToLowerInvariant() switch
                {
                    "left" => TableCellAlignment.Left,
                    "center" => TableCellAlignment.Center,
                    "right" => TableCellAlignment.Right,
                    _ => TableCellAlignment.None
                };

                foreach (var candidate in table.Rows)
                {
                    if (column < candidate.Cells.Count)
                    {
                        candidate.Cells[column].Alignment = alignment;
                        candidate.Cells[column].Pristine = false;
                    }
                }

                return Commit(document, cell, cursor.Offset);
            default:
                return null;
        }
    }

    private MarkdownEditorUpdate DeleteTable(Document document, Table table) => RemoveAndMove(document, table, forward: true);
}
