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
    }

    private sealed record Cursor(Block Block, IBlockInlineContainer? Content, int Offset, bool AfterMarker = false);

    private readonly List<Edit> undo = [];
    private readonly List<Edit> redo = [];
    private (int Start, int End) pending;

    public MarkdownEditorEngine(string? text)
    {
        Text = Normalize(text);
    }

    public string Text { get; private set; }

    public bool RenderHtml { get; set; } = true;

    public bool CanUndo => undo.Count > 0;

    public bool CanRedo => redo.Count > 0;

    public static string Normalize(string? text) => (text ?? string.Empty).Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\r", "\n", StringComparison.Ordinal);

    public void Reset(string? text)
    {
        Text = Normalize(text);
        undo.Clear();
        redo.Clear();
    }

    public MarkdownEditorUpdate Apply(int start, int end, string inserted, (int Start, int End) after, string? key = null, bool merge = false, (int Start, int End)? before = null)
    {
        (start, end) = Clamp(start, end);

        if (Text[start..end] == inserted)
        {
            return Render(after.Start, after.End);
        }

        var edit = new Edit
        {
            Start = start,
            Removed = Text[start..end],
            Inserted = inserted,
            Before = before ?? (start, end),
            After = after,
            Key = key
        };

        Text = Text[..start] + inserted + Text[end..];
        redo.Clear();

        var last = undo.Count > 0 ? undo[^1] : null;

        if (merge && key != null && last?.Key == key && Merge(last, edit))
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

        return Render(after.Start, after.End);
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
        redo.Add(edit);

        return Render(edit.Before.Start, edit.Before.End, includeText: true);
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
        undo.Add(edit);

        return Render(edit.After.Start, edit.After.End, includeText: true);
    }

    private (int Start, int End) Clamp(int start, int end)
    {
        start = Math.Clamp(start, 0, Text.Length);
        end = Math.Clamp(end, start, Text.Length);
        return (start, end);
    }

    public MarkdownEditorUpdate Render(int selectionStart, int selectionEnd, bool includeText = false)
    {
        (selectionStart, selectionEnd) = Clamp(selectionStart, selectionEnd);

        var document = MarkdownParser.Parse(Text);
        var rendered = RenderHtml ? HtmlVisitor.Render(document, Text) : null;
        var segments = rendered == null ? null : new int[rendered.Segments.Count * 3];

        for (var index = 0; rendered != null && index < rendered.Segments.Count; index++)
        {
            var segment = rendered.Segments[index];
            segments![index * 3] = segment.SourceStart;
            segments[index * 3 + 1] = segment.SourceEnd;
            segments[index * 3 + 2] = segment.Length;
        }

        return new MarkdownEditorUpdate
        {
            Html = rendered?.Html,
            Segments = segments,
            Text = includeText ? Text : null,
            SelectionStart = selectionStart,
            SelectionEnd = selectionEnd,
            State = State(Inflate(document), selectionStart, selectionEnd)
        };
    }

    public MarkdownEditorToolState State(int selectionStart, int selectionEnd)
    {
        (selectionStart, selectionEnd) = Clamp(selectionStart, selectionEnd);
        return State(Parse(), selectionStart, selectionEnd);
    }

    private MarkdownEditorToolState State(Document document, int start, int end)
    {
        var formats = new List<string>();
        var chain = Chain(document, start);
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

        var cursor = Resolve(document, start);

        if (cursor.Content != null)
        {
            var runs = InlineContent.Flatten(cursor.Content.Children);
            var (index, offset) = InlineContent.Locate(runs, cursor.Offset);

            if (index < runs.Count && runs[index].Atom == null && (offset > 0 || index == 0 || runs[index - 1].Atom != null) && (offset < runs[index].Length || index == runs.Count - 1))
            {
                formats.AddRange(runs[index].Marks.Select(FormatOf).OfType<string>());
            }
            else if (index > 0 && offset == 0 && runs[index - 1].Atom == null)
            {
                formats.AddRange(runs[index - 1].Marks.Select(FormatOf).OfType<string>());
            }
        }

        string? kind = null;
        var first = true;

        foreach (var candidate in Leaves(document, start, end).Where(leaf => leaf is Paragraph or Heading).Select(Kind))
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

    private static IEnumerable<Leaf> Leaves(BlockContainer container, int start, int end)
    {
        foreach (var block in container.Children)
        {
            if (block.SourceEnd < start || block.SourceStart > end)
            {
                continue;
            }

            if (block is Leaf leaf)
            {
                yield return leaf;
            }
            else if (block is BlockContainer nested)
            {
                foreach (var inner in Leaves(nested, start, end))
                {
                    yield return inner;
                }
            }
        }
    }

    private static string? Kind(Leaf leaf) => leaf switch
    {
        Heading heading => "h" + heading.Level.ToString(CultureInfo.InvariantCulture),
        Paragraph => "p",
        _ => null
    };

    private Document Parse() => Inflate(MarkdownParser.Parse(Text));

    private Document Inflate(Document document)
    {
        Inflate(document, document);
        return document;
    }

    private void Inflate(Document document, BlockContainer container)
    {
        var start = container is Document ? 0 : container.SourceStart;
        var end = container is Document ? Text.Length : container.SourceEnd;
        var children = container.Children.ToList();
        var index = 0;
        var insertAt = 0;

        foreach (var child in children)
        {
            var placeholders = BlankLines.Placeholders(Text, start, child.SourceStart, index > 0, true);

            if (index == 0 && placeholders.Count == 0 && container is Document && BlankLines.IsSealed(child) && child.SourceStart == 0)
            {
                container.Insert(insertAt++, new Paragraph { Virtual = true, SourceStart = 0, SourceEnd = 0 });
            }

            foreach (var offset in placeholders)
            {
                container.Insert(insertAt++, new Paragraph { SourceStart = offset, SourceEnd = offset, Pristine = true });
            }

            if (container is Document && BlankLines.IsSealed(child) && index > 0 && placeholders.Count == 0 && children[index - 1] is not Paragraph)
            {
                var gap = child.SourceStart >= 2 && Text[child.SourceStart - 1] == '\n' && Text[child.SourceStart - 2] == '\n' ? child.SourceStart - 1 : child.SourceStart;
                container.Insert(insertAt++, new Paragraph { Virtual = true, SourceStart = gap, SourceEnd = gap });
            }

            insertAt++;

            if (child is BlockContainer nested)
            {
                Inflate(document, nested);
            }

            start = child.SourceEnd;
            index++;
        }

        var trailing = BlankLines.Placeholders(Text, start, end, index > 0, false);

        foreach (var offset in trailing)
        {
            container.Insert(insertAt++, new Paragraph { SourceStart = offset, SourceEnd = offset, Pristine = true });
        }

        if (trailing.Count == 0 && container is Document && container.LastChild is { } last && BlankLines.IsSealed(last))
        {
            container.Insert(insertAt, new Paragraph { Virtual = true, SourceStart = Text.Length, SourceEnd = Text.Length });
        }
    }

    private static List<Block> Chain(Document document, int offset)
    {
        var chain = new List<Block>();
        BlockContainer container = document;

        while (true)
        {
            var child = Nearest(container.Children, offset);

            if (child == null)
            {
                break;
            }

            chain.Add(child);

            if (child is BlockContainer next)
            {
                container = next;
            }
            else
            {
                break;
            }
        }

        return chain;
    }

    private static Block? Nearest(IReadOnlyList<Block> blocks, int offset)
    {
        foreach (var block in blocks)
        {
            if (block is Paragraph { Children.Count: 0 } empty && empty.SourceStart == offset)
            {
                return block;
            }
        }

        foreach (var block in blocks)
        {
            if (block is Paragraph { Children.Count: 0 })
            {
                continue;
            }

            if (block.SourceStart <= offset && (offset < block.SourceEnd || (offset == block.SourceEnd && !BlankLines.IsSealed(block))))
            {
                return block;
            }
        }

        return null;
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

    private Cursor Resolve(Document document, int offset)
    {
        if (AfterTrailingBreak(document, offset) is { } continued)
        {
            return continued;
        }

        var chain = Chain(document, offset);
        var block = chain.LastOrDefault();

        if (block is null or BlockContainer)
        {
            var lineEnd = Text.IndexOf('\n', offset) is var newline && newline >= 0 ? newline : Text.Length;
            var empty = Blocks(document).OfType<BlockContainer>().Where(candidate => candidate.Children.Count == 0 && candidate.SourceEnd < offset && Text[candidate.SourceEnd..lineEnd].Trim().Length == 0).OrderByDescending(candidate => candidate.SourceEnd).FirstOrDefault();

            if (empty != null)
            {
                return Resolve(document, empty.SourceEnd);
            }
        }

        if (block == null)
        {
            var lineStart = offset == 0 ? 0 : Text.LastIndexOf('\n', offset - 1) + 1;
            var lineEnd = Text.IndexOf('\n', offset) is var newline && newline >= 0 ? newline : Text.Length;

            if (Text[lineStart..lineEnd].Trim().Length == 0)
            {
                var index = 0;

                while (index < document.Children.Count && document.Children[index].SourceEnd <= offset && !(document.Children[index] is Paragraph { Children.Count: 0 } && document.Children[index].SourceStart > offset))
                {
                    index++;
                }

                var created = new Paragraph { SourceStart = offset, SourceEnd = offset };
                document.Insert(index, created);
                return new Cursor(created, created, 0);
            }

            var neighbour = Blocks(document).Where(candidate => candidate.SourceEnd <= offset).OrderBy(candidate => offset - candidate.SourceEnd).FirstOrDefault()
                ?? Blocks(document).OrderBy(candidate => candidate.SourceStart).FirstOrDefault();

            if (neighbour == null)
            {
                var paragraph = new Paragraph { SourceStart = 0, SourceEnd = 0 };
                document.Add(paragraph);
                return new Cursor(paragraph, paragraph, 0);
            }

            return neighbour is IBlockInlineContainer content ? new Cursor(neighbour, content, InlineContent.Length(Runs(content))) : new Cursor(neighbour, null, 0);
        }

        switch (block)
        {
            case Paragraph or Heading:
                var leaf = (Leaf)block;
                return new Cursor(leaf, leaf, ContentOffset(leaf, offset, out var afterMarker), afterMarker);
            case Table table:
                var cells = table.Rows.SelectMany(row => row.Cells).Where(cell => CellRange(cell).Start >= 0).ToList();
                var hit = cells.FirstOrDefault(cell => CellRange(cell) is var range && offset >= range.Start && offset <= range.End);

                if (hit != null)
                {
                    return new Cursor(table, hit, ContentOffset(hit, offset, out var afterCellMarker), afterCellMarker);
                }

                var nearestCell = cells.OrderBy(cell => Math.Abs(CellRange(cell).Start - offset)).FirstOrDefault();
                return nearestCell != null ? new Cursor(table, nearestCell, offset >= CellRange(nearestCell).End ? InlineContent.Length(InlineContent.Flatten(nearestCell.Children)) : 0) : new Cursor(table, null, 0);
            case FencedCodeBlock or IndentedCodeBlock or HtmlBlock:
                var code = (Leaf)block;
                return new Cursor(code, null, Math.Clamp(code.Content.ToContent(offset), 0, code.Value.Length));
            case ListItem { FirstChild: Paragraph or Heading } item:
                var firstLeaf = (Leaf)item.FirstChild;
                return new Cursor(firstLeaf, firstLeaf, 0);
            case ListItem item:
                return new Cursor(item, null, 0);
            default:
                var inner = Blocks(document).Where(candidate => candidate is Paragraph or Heading && candidate.SourceStart <= offset).OrderByDescending(candidate => candidate.SourceStart).FirstOrDefault() as Leaf;
                return inner != null ? new Cursor(inner, inner, InlineContent.Length(InlineContent.Flatten(inner.Children))) : new Cursor(block, null, 0);
        }
    }

    private Cursor? AfterTrailingBreak(Document document, int offset)
    {
        var lineStart = offset == 0 ? 0 : Text.LastIndexOf('\n', offset - 1) + 1;
        var lineEnd = Text.IndexOf('\n', offset) is var newline && newline >= 0 ? newline : Text.Length;

        if (lineStart == 0 || Text[lineStart..lineEnd].Any(ch => ch is not (' ' or '\t' or '>')) || Text[offset..lineEnd].Trim().Length > 0)
        {
            return null;
        }

        var previousEnd = lineStart - 1;
        var previousStart = previousEnd == 0 ? 0 : Text.LastIndexOf('\n', previousEnd - 1) + 1;
        var previousLine = Text[previousStart..previousEnd];
        var backslash = previousLine.EndsWith('\\');

        if (!backslash && !previousLine.EndsWith("  ", StringComparison.Ordinal))
        {
            return null;
        }

        var paragraph = Blocks(document).OfType<Paragraph>().LastOrDefault(candidate => candidate.Children.Count > 0 && candidate.SourceEnd >= previousStart && candidate.SourceEnd <= previousEnd);

        if (paragraph == null)
        {
            return null;
        }

        if (backslash && paragraph.Children[^1] is Text text && text.Value.EndsWith('\\'))
        {
            text.Value = text.Value[..^1];
        }

        paragraph.Add(new LineBreak { Backslash = backslash });
        Touch(paragraph);
        return new Cursor(paragraph, paragraph, InlineContent.Length(Runs(paragraph)));
    }

    private List<Run> Runs(IBlockInlineContainer content)
    {
        var runs = InlineContent.Flatten(content.Children);

        if (content is Leaf { Pristine: true } leaf && runs.Count > 0 && leaf.Children[^1] is { } last && last.SourceEnd > last.SourceStart && last.SourceEnd <= Text.Length)
        {
            var end = last.SourceEnd;

            while (end < Text.Length && Text[end] is ' ' or '\t')
            {
                end++;
            }

            if (end > last.SourceEnd && (end == Text.Length || Text[end] == '\n'))
            {
                runs.Add(new Run(Text[last.SourceEnd..end], [], null));
            }
        }

        return runs;
    }

    private int ContentOffset(IBlockInlineContainer content, int offset, out bool afterMarker)
    {
        var runs = Runs(content);
        var position = 0;
        Inline? previousOrigin = null;
        afterMarker = false;

        foreach (var run in runs)
        {
            var origin = run.Atom ?? run.Origin;
            afterMarker = origin != null && previousOrigin != null && previousOrigin.SourceEnd < offset && offset == origin.SourceStart;
            previousOrigin = origin ?? previousOrigin;

            if (origin == null)
            {
                if (run == runs[^1] && run.Origin == null && run.Atom == null)
                {
                    var previousEnd = runs.Count > 1 && (runs[^2].Atom ?? runs[^2].Origin) is { } beforeTrailing ? beforeTrailing.SourceEnd : offset;
                    return position + Math.Clamp(offset - previousEnd, 0, run.Length);
                }

                position += run.Length;
                continue;
            }

            if (offset < origin.SourceStart)
            {
                return position;
            }

            if (offset <= origin.SourceEnd)
            {
                if (run.Atom != null)
                {
                    return offset < origin.SourceEnd ? position : position + 1;
                }

                if (origin is Text text)
                {
                    return position + MarkdownWriter.ValueOffsetWithin(Text, text, offset - text.SourceStart);
                }

                if (origin is Code code)
                {
                    var ticks = 0;

                    while (code.SourceStart + ticks < code.SourceEnd && Text[code.SourceStart + ticks] == '`')
                    {
                        ticks++;
                    }

                    var padded = code.SourceEnd - code.SourceStart >= code.Value.Length + 2 * ticks + 2 && Text[code.SourceStart + ticks] == ' ';
                    return position + Math.Clamp(offset - code.SourceStart - ticks - (padded ? 1 : 0), 0, code.Value.Length);
                }
            }

            position += run.Length;
        }

        return position;
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
        FencedCodeBlock or IndentedCodeBlock or HtmlBlock => Commit(document, block, ((Leaf)block).Value.TrimEnd('\n').Length),
        IBlockInlineContainer content => Commit(document, content, InlineContent.Length(InlineContent.Flatten(content.Children))),
        BlockContainer container when LastLeaf(container) is { } leaf => CommitAtEnd(document, leaf),
        _ => Commit(document, block, 0)
    };

    private MarkdownEditorUpdate Commit(Document document, INode caretNode, int caretOffset, INode? endNode = null, int endOffset = 0, string? key = null, bool merge = false, bool afterMarker = false)
    {
        NormalizeLists(document);
        var writer = MarkdownWriter.Preserve(document, Text);
        var replacement = writer.Text;
        var start = Offset(writer, caretNode, caretOffset, preferNext: endNode != null || afterMarker);
        var end = endNode == null ? start : Offset(writer, endNode, endOffset);

        if (replacement == Text)
        {
            return Render(Math.Min(start, end), Math.Max(start, end));
        }

        var prefix = 0;

        while (prefix < Text.Length && prefix < replacement.Length && Text[prefix] == replacement[prefix] && prefix < Math.Min(start, end))
        {
            prefix++;
        }

        var suffix = 0;

        while (suffix < Text.Length - prefix && suffix < replacement.Length - prefix && Text[Text.Length - 1 - suffix] == replacement[replacement.Length - 1 - suffix] && replacement.Length - suffix > Math.Max(start, end))
        {
            suffix++;
        }

        return Apply(prefix, Text.Length - suffix, replacement[prefix..(replacement.Length - suffix)], (Math.Min(start, end), Math.Max(start, end)), key, merge, pending);
    }

    private int Offset(MarkdownWriter writer, INode node, int offset, bool preferNext = false)
    {
        switch (node)
        {
            case Text text:
                return writer.OffsetWithin(text, offset);
            case Code code:
                return writer.Positions[code].Start + Math.Clamp(offset, 0, code.Value.Length);
            case FencedCodeBlock or IndentedCodeBlock or HtmlBlock:
                return writer.OffsetWithin((Block)node, offset);
            case IBlockInlineContainer content:
                return ContentPosition(writer, content, offset, preferNext);
            default:
                return writer.Positions.TryGetValue(node, out var position) ? position.Start : 0;
        }
    }

    private int ContentPosition(MarkdownWriter writer, IBlockInlineContainer content, int offset, bool preferNext = false)
    {
        var runs = InlineContent.Flatten(content.Children);

        if (runs.Count == 0)
        {
            return writer.Positions.TryGetValue(content, out var empty) ? empty.Start : Fallback(writer, content is Block block ? block : null, content is Leaf leaf ? leaf.SourceStart : 0);
        }

        if (offset == 0 && !preferNext && content is Paragraph && writer.Positions.TryGetValue(content, out var blockStart))
        {
            return blockStart.Start;
        }

        var position = 0;

        for (var index = 0; index < runs.Count; index++)
        {
            var run = runs[index];
            var node = run.Atom ?? run.Origin;
            var runEnd = position + run.Length;
            var inside = offset < runEnd || (offset == runEnd && (index == runs.Count - 1 || (run.Atom == null && !preferNext)));

            if (offset >= position && inside)
            {
                if (node == null)
                {
                    position += run.Length;
                    continue;
                }

                if (writer.Positions.TryGetValue(node, out var located))
                {
                    if (run.Atom != null)
                    {
                        return offset > position ? located.End : located.Start;
                    }

                    return node is Text text ? writer.OffsetWithin(text, offset - position) : located.Start + Math.Clamp(offset - position, 0, run.Length);
                }

                var sourceOffset = node is Text pristine ? pristine.SourceStart + MarkdownWriter.SourceOffsetWithin(Text, pristine, offset - position) : node.SourceStart + (offset - position);
                return Fallback(writer, content is Block owner ? owner : null, sourceOffset);
            }

            position += run.Length;
        }

        var last = runs[^1].Atom ?? runs[^1].Origin;

        if (last != null && writer.Positions.TryGetValue(last, out var end))
        {
            return end.End;
        }

        return Fallback(writer, content is Block lastOwner ? lastOwner : null, last?.SourceEnd ?? 0);
    }

    private int Fallback(MarkdownWriter writer, Block? block, int sourceOffset)
    {
        var top = block;

        while (top != null && top.Parent is not Document && top.Parent != null)
        {
            top = top.Parent;
        }

        if (top != null && writer.Positions.TryGetValue(top, out var position) && sourceOffset >= top.SourceStart)
        {
            return position.Start + (sourceOffset - top.SourceStart);
        }

        return Math.Clamp(sourceOffset, 0, writer.Text.Length);
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

    public MarkdownEditorUpdate InsertText(int start, int end, string text, bool literal, string? key = null, bool merge = false, bool paragraphs = false, bool selection = false)
    {
        (start, end) = Clamp(start, end);
        pending = selection ? (start, end) : (end, end);

        if (!literal)
        {
            var caret = start + text.Length;
            return Apply(start, end, text, (caret, caret), key, merge);
        }

        var document = Parse();
        var cursor = start < end ? DeleteRange(document, start, end) : Resolve(document, start);

        if (cursor == null)
        {
            return Render(start, start);
        }

        return InsertAt(document, cursor, text, paragraphs, key, merge) ?? Render(start, start);
    }

    private MarkdownEditorUpdate? InsertAt(Document document, Cursor cursor, string text, bool paragraphs, string? key, bool merge)
    {
        text = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\r", "\n", StringComparison.Ordinal);

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

        var runs = Runs(cursor.Content);
        SetRuns(cursor.Content, InlineContent.Insert(runs, cursor.Offset, lines[0], preferRight: cursor.AfterMarker));
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

    private Cursor? DeleteRange(Document document, int start, int end)
    {
        var first = Resolve(document, start);
        var last = Resolve(document, end);

        if (first.Block == last.Block && first.Content == last.Content)
        {
            if (first.Block is FencedCodeBlock or IndentedCodeBlock or HtmlBlock)
            {
                var code = (Leaf)first.Block;

                if (start < code.Content.ToSource(0) || end > code.Content.ToSourceEnd(code.Value.Length))
                {
                    return null;
                }

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

        var firstCovered = first.Block.SourceStart >= start && first.Block.SourceEnd <= end && first.Block is not Paragraph { Children.Count: 0 };
        var lastCovered = last.Block.SourceStart >= start && last.Block.SourceEnd <= end && last.Block is not Paragraph { Children.Count: 0 };

        if ((BlankLines.IsSealed(first.Block) && !firstCovered) || (BlankLines.IsSealed(last.Block) && !lastCovered) || first.Content is TableCell || last.Content is TableCell)
        {
            return null;
        }

        var covered = Blocks(document).Where(block => block != first.Block && block != last.Block && block.SourceStart >= first.Block.SourceStart && block.SourceEnd <= last.Block.SourceEnd && !IsAncestor(block, first.Block) && !IsAncestor(block, last.Block)).ToList();

        if (covered.Any(block => BlankLines.IsSealed(block) && (block.SourceStart < start || block.SourceEnd > end)))
        {
            return null;
        }

        var target = first.Content as Leaf;

        if (target != null && !firstCovered)
        {
            SetRuns(target, InlineContent.Split(Runs(target), first.Offset, out _));
        }

        if (last.Content is Leaf lastLeaf && !lastCovered)
        {
            InlineContent.Split(Runs(lastLeaf), last.Offset, out var tail);

            if (target != null && !firstCovered)
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

        if (firstCovered)
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

        if (target is Heading { Children.Count: 0 } emptyHeading && emptyHeading.Parent != null)
        {
            var paragraph = new Paragraph();
            emptyHeading.Parent.Replace(emptyHeading, paragraph);
            Touch(paragraph);
            return new Cursor(paragraph, paragraph, 0);
        }

        if (target != null && target.Parent != null)
        {
            return new Cursor(target, target, first.Offset);
        }

        if (last.Content is Leaf remaining && remaining.Parent != null)
        {
            return new Cursor(remaining, remaining, 0);
        }

        var replacement = new Paragraph();

        if (document.Children.Count == 0)
        {
            document.Add(replacement);
        }
        else
        {
            var anchor = document.Children.FirstOrDefault(block => block.SourceStart >= start) ?? document.Children[^1];
            InsertBefore(anchor, replacement);
        }

        Touch(replacement);
        return new Cursor(replacement, replacement, 0);
    }

    private static (int Start, int End) CellRange(TableCell cell)
    {
        var start = cell.Children.Count > 0 ? cell.Children[0].SourceStart : cell.Content.Segments.Count > 0 ? cell.Content.Segments[0].SourceStart : -1;
        return (start, cell.Children.Count > 0 ? cell.Children[^1].SourceEnd : start);
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

    private Leaf SplitLeaf(Leaf leaf, int offset)
    {
        var runs = InlineContent.Flatten(leaf.Children);
        var head = InlineContent.Split(runs, offset, out var tail);
        SetRuns(leaf, TrimEnd(head));
        Leaf second = leaf is Heading heading ? new AtxHeading { Level = heading.Level } : new Paragraph();
        second.ReplaceInlines(InlineContent.Rebuild(TrimStart(tail)));
        InsertAfter(leaf, second);
        return second;
    }

    private static List<Run> TrimEnd(List<Run> runs)
    {
        while (runs.Count > 0 && runs[^1].Atom == null && runs[^1].Text.TrimEnd().Length < runs[^1].Text.Length)
        {
            var last = runs[^1];
            var trimmed = last.Text.TrimEnd();

            if (trimmed.Length == 0)
            {
                runs.RemoveAt(runs.Count - 1);
                continue;
            }

            runs[^1] = new Run(trimmed, last.Marks, null);
            break;
        }

        return runs;
    }

    private static List<Run> TrimStart(List<Run> runs)
    {
        while (runs.Count > 0 && runs[0].Atom == null && runs[0].Text.TrimStart().Length < runs[0].Text.Length)
        {
            var first = runs[0];
            var trimmed = first.Text.TrimStart();

            if (trimmed.Length == 0)
            {
                runs.RemoveAt(0);
                continue;
            }

            runs[0] = new Run(trimmed, first.Marks, null);
            break;
        }

        return runs;
    }

    public MarkdownEditorUpdate? Delete(int start, int end, bool forward = false, string? key = null, bool merge = false, bool selection = false)
    {
        (start, end) = Clamp(start, end);
        pending = selection ? (start, end) : forward ? (start, start) : (end, end);
        var document = Parse();

        if (!selection && end - start > 1)
        {
            var from = Resolve(document, start);
            var to = Resolve(document, end);

            if (from.Block != to.Block && to.Content != null && to.Offset == 0 && (from.Content == null || from.Offset >= InlineContent.Length(Runs(from.Content))))
            {
                if (!forward)
                {
                    return JoinBackward(document, to.Block, to.Content);
                }

                if (from.Content != null)
                {
                    return JoinForward(document, from.Block, from.Content);
                }
            }
        }

        if (selection || end - start > 1)
        {
            var cursor = DeleteRange(document, start, end);
            return cursor == null ? null : Commit(document, cursor.Content ?? (INode)cursor.Block, cursor.Offset);
        }

        var caret = Resolve(document, forward ? start : end);

        if (caret.Block is FencedCodeBlock or IndentedCodeBlock or HtmlBlock)
        {
            var code = (Leaf)caret.Block;

            if (forward ? caret.Offset >= code.Value.TrimEnd('\n').Length : caret.Offset == 0)
            {
                if (code.Value.Trim().Length > 0)
                {
                    return null;
                }

                var index = code.Parent.IndexOf(code);
                var previousBlock = code.Parent.Children.Take(index).LastOrDefault(block => block is not Paragraph { Virtual: true });
                var nextBlock = code.Parent.Children.Skip(index + 1).FirstOrDefault(block => block is not Paragraph { Virtual: true });
                var neighbour = forward ? nextBlock ?? previousBlock : previousBlock ?? nextBlock;
                RemoveBlock(code);

                if (neighbour == null)
                {
                    var replacement = new Paragraph();
                    document.Add(replacement);
                    Touch(replacement);
                    return Commit(document, replacement, 0);
                }

                return neighbour == previousBlock ? CommitAtEnd(document, neighbour) : Commit(document, neighbour is IBlockInlineContainer content ? content : neighbour, 0);
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

        var (deletedRun, within) = InlineContent.Locate(runs, deleteStart + 1);
        var startsRun = within == 1 && deletedRun > 0 && !runs[deletedRun].Marks.SequenceEqual(runs[deletedRun - 1].Marks);
        SetRuns(caret.Content, InlineContent.Delete(runs, deleteStart, deleteEnd));
        return Commit(document, caret.Content, deleteStart, key: key, merge: merge, afterMarker: startsRun);
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
                return Commit(document, target, InlineContent.Length(InlineContent.Flatten(target.Children)));
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

        if (previous is Paragraph { Virtual: true } && index > 1)
        {
            previous = container.Children[index - 2];
        }

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
            return Render(EndOf(previous), EndOf(previous));
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

    private int JoinLeaves(Leaf target, Leaf source)
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

        if (next is Paragraph { Virtual: true } skipped)
        {
            next = NextBlock(skipped);
        }

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

        if (next == null)
        {
            return null;
        }

        if (next is Paragraph { Virtual: true })
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
            return Render(StartOf(next), StartOf(next));
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
        (start, end) = Clamp(start, end);
        var document = Parse();
        var cursor = start < end ? DeleteRange(document, start, end) : Resolve(document, start);

        if (cursor == null)
        {
            return null;
        }

        pending = (start, start);

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
            var withBreak = new List<Run>();
            var position = 0;
            var inserted = false;

            foreach (var run in leafRuns)
            {
                if (!inserted && cursor.Offset <= position + run.Length)
                {
                    var within = Math.Clamp(cursor.Offset - position, 0, run.Length);
                    var lineBreak = new Run(new LineBreak { Backslash = false }, run.Marks);

                    if (run.Atom != null)
                    {
                        withBreak.Add(within == 0 ? lineBreak : run);
                        withBreak.Add(within == 0 ? run : lineBreak);
                    }
                    else
                    {
                        withBreak.Add(run.Slice(0, within));
                        withBreak.Add(lineBreak);
                        withBreak.Add(run.Slice(within, run.Length));
                    }

                    inserted = true;
                }
                else
                {
                    withBreak.Add(run);
                }

                position += run.Length;
            }

            if (!inserted)
            {
                withBreak.Add(new Run(new LineBreak { Backslash = false }, []));
            }

            SetRuns(leaf, withBreak);
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

    private static int EndOf(Block block) => block switch
    {
        Table table => table.Rows.SelectMany(row => row.Cells).Select(cell => CellRange(cell).End).Where(end => end >= 0).DefaultIfEmpty(table.SourceEnd).Max(),
        Leaf { Value.Length: > 0 } code => code.Content.ToSourceEnd(code.Value.TrimEnd('\n').Length),
        _ => block.SourceEnd
    };

    private static int StartOf(Block block) => block switch
    {
        Table table => table.Rows.SelectMany(row => row.Cells).Select(cell => CellRange(cell).Start).Where(start => start >= 0).DefaultIfEmpty(table.SourceStart).Min(),
        Leaf { Value.Length: > 0 } code => code.Content.ToSource(0),
        _ => block.SourceStart
    };

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

        var added = new TableRow();

        for (var index = 0; index < table.Rows[0].Cells.Count; index++)
        {
            added.Add(string.Empty, table.Rows[0].Cells[index].Alignment);
            added.Cells[index].Pristine = false;
        }

        table.InsertRow(rowIndex + 1, added);
        Touch(table);
        return Commit(document, added.Cells[sameColumn ? Math.Min(column, added.Cells.Count - 1) : 0], 0);
    }

    public MarkdownEditorUpdate? AppendRow(int caret)
    {
        (caret, _) = Clamp(caret, caret);
        var document = Parse();
        var cursor = Resolve(document, caret);
        pending = (caret, caret);
        return cursor.Block is Table table && cursor.Content is TableCell cell ? InsertRow(document, table, cell, sameColumn: false) : null;
    }

    public MarkdownEditorUpdate? Indent(int start, int end, bool outdent)
    {
        (start, end) = Clamp(start, end);
        pending = (start, end);
        var document = Parse();
        var cursor = Resolve(document, start);
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
        (start, end) = Clamp(start, end);
        var document = Parse();
        pending = (start, end);

        switch (name)
        {
            case MarkdownEditorCommands.Bold:
            case MarkdownEditorCommands.Italic:
            case MarkdownEditorCommands.Strikethrough:
            case MarkdownEditorCommands.Code:
                return ToggleMark(document, name, start, end);
            case MarkdownEditorCommands.FormatBlock:
                return FormatBlock(document, start, end, value);
            case MarkdownEditorCommands.Quote:
                return ToggleQuote(document, start, end);
            case MarkdownEditorCommands.UnorderedList:
            case MarkdownEditorCommands.OrderedList:
            case MarkdownEditorCommands.TaskList:
                return ToggleList(document, name, start, end);
            case MarkdownEditorCommands.CodeBlock:
                return ToggleCodeBlock(document, start);
            case MarkdownEditorCommands.HorizontalRule:
                return InsertRule(document, end);
            case MarkdownEditorCommands.Link:
            case MarkdownEditorCommands.Image:
                return InsertLink(document, name == MarkdownEditorCommands.Image, start, end, value, label);
            case MarkdownEditorCommands.InsertText:
                return InsertMarkdown(document, start, end, value ?? string.Empty);
            case MarkdownEditorCommands.InsertTable:
                return InsertTable(document, start, value);
            case MarkdownEditorCommands.TableRowBefore:
            case MarkdownEditorCommands.TableRowAfter:
            case MarkdownEditorCommands.TableColumnBefore:
            case MarkdownEditorCommands.TableColumnAfter:
            case MarkdownEditorCommands.TableDeleteRow:
            case MarkdownEditorCommands.TableDeleteColumn:
            case MarkdownEditorCommands.TableDelete:
            case MarkdownEditorCommands.TableAlign:
                return TableCommand(document, name, start, value);
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

    private MarkdownEditorUpdate? ToggleMark(Document document, string name, int start, int end)
    {
        var kind = MarkKindOf(name);
        var first = Resolve(document, start);
        var last = Resolve(document, end);

        if (first.Content == null || first.Block is FencedCodeBlock or IndentedCodeBlock or HtmlBlock)
        {
            return null;
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

        var targets = Targets(document, start, end, first, last);

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

    private static List<(IBlockInlineContainer Content, int Start, int End)> Targets(Document document, int start, int end, Cursor first, Cursor last)
    {
        var targets = new List<(IBlockInlineContainer Content, int Start, int End)>();

        if (first.Content == last.Content)
        {
            targets.Add((first.Content!, Math.Min(first.Offset, last.Offset), Math.Max(first.Offset, last.Offset)));
            return targets;
        }

        foreach (var leaf in Leaves(document, start, end).Where(leaf => leaf is Paragraph or Heading))
        {
            var from = leaf == first.Content ? first.Offset : 0;
            var to = leaf == last.Content ? last.Offset : InlineContent.Length(InlineContent.Flatten(leaf.Children));

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

    private static List<Block> TopLevelBlocks(Document document, int start, int end)
    {
        return document.Children.Where(block => block.SourceStart <= end && block.SourceEnd >= start && block is not Paragraph { Virtual: true }).ToList();
    }

    private MarkdownEditorUpdate? FormatBlock(Document document, int start, int end, string? value)
    {
        var level = value is ['h', >= '1' and <= '6'] ? value[1] - '0' : 0;
        var cursor = Resolve(document, start);
        var leaves = (start == end ? (cursor.Content is Leaf single ? [single] : new List<Leaf>()) : Leaves(document, start, end).ToList()).Where(leaf => leaf is Paragraph or Heading).ToList();
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
            var last = Resolve(document, end);
            var endLeaf = last.Content is Leaf lastOriginal && replacements.TryGetValue(lastOriginal, out var lastReplaced) ? lastReplaced : last.Content as Leaf;
            return Commit(document, caretLeaf, cursor.Content is Leaf ? cursor.Offset : 0, endLeaf, endLeaf != null ? last.Offset : 0);
        }

        return Commit(document, caretLeaf, cursor.Content is Leaf ? cursor.Offset : 0);
    }

    private MarkdownEditorUpdate? ToggleQuote(Document document, int start, int end)
    {
        var cursor = Resolve(document, start);
        var last = Resolve(document, end);
        var chain = Chain(document, start);
        var quote = chain.OfType<BlockQuote>().LastOrDefault();
        INode caretNode = cursor.Content ?? (INode)cursor.Block;

        if (quote != null && (start == end || Chain(document, end).Contains(quote)))
        {
            Splice(quote.Parent, quote.Parent.IndexOf(quote), quote.Children.ToList());
            RemoveBlock(quote);
            return Commit(document, caretNode, cursor.Offset);
        }

        var blocks = start == end ? [TopLevel(cursor.Block)] : TopLevelBlocks(document, start, end);

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

    private MarkdownEditorUpdate? ToggleList(Document document, string name, int start, int end)
    {
        var cursor = Resolve(document, start);
        var selectionEnd = Resolve(document, end);
        var chain = Chain(document, start);
        var item = chain.OfType<ListItem>().LastOrDefault();
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
            var selected = list.Children.OfType<ListItem>().Where(candidate => candidate.SourceStart <= end && candidate.SourceEnd >= start).ToList();
            var caretLeaf = cursor.Content as Leaf;
            var endLeaf = selectionEnd.Content as Leaf;
            LiftItems(list, selected);
            return Commit(document, caretLeaf ?? caretNode, cursor.Offset, endLeaf, selectionEnd.Offset);
        }

        var blocks = (start == end ? [TopLevel(cursor.Block)] : TopLevelBlocks(document, start, end)).Where(block => block is not List).ToList();

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

    private MarkdownEditorUpdate? ToggleCodeBlock(Document document, int start)
    {
        var cursor = Resolve(document, start);

        if (cursor.Block is FencedCodeBlock or IndentedCodeBlock)
        {
            var code = (Leaf)cursor.Block;
            var parsed = MarkdownParser.Parse(code.Value.TrimEnd('\n'));
            var blocks = parsed.Children.Count > 0 ? parsed.Children.ToList() : [new Paragraph()];
            Splice(code.Parent, code.Parent.IndexOf(code), blocks);
            RemoveBlock(code);
            var target = blocks[0] as IBlockInlineContainer;
            return target != null ? Commit(document, target, Math.Min(cursor.Offset, InlineContent.Length(InlineContent.Flatten(target.Children)))) : Commit(document, blocks[0], 0);
        }

        if (cursor.Content is not Leaf leaf)
        {
            return null;
        }

        var text = leaf.Pristine && leaf.Children.Count > 0 && leaf.Children[^1].SourceEnd > leaf.Children[0].SourceStart ? Text[leaf.Children[0].SourceStart..leaf.Children[^1].SourceEnd] : InlineContent.PlainText(InlineContent.Flatten(leaf.Children));
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

    private MarkdownEditorUpdate InsertRule(Document document, int caret)
    {
        var cursor = Resolve(document, caret);
        var rule = new ThematicBreak();

        if (cursor.Content is Paragraph { Children.Count: 0 } empty && empty.Parent is Document)
        {
            empty.Virtual = false;
            InsertBefore(empty, rule);
            return Commit(document, empty, 0);
        }

        if (cursor.Content is Paragraph inside && inside.Parent is Document && cursor.Offset > 0 && cursor.Offset < InlineContent.Length(Runs(inside)))
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

    private MarkdownEditorUpdate? InsertLink(Document document, bool image, int start, int end, string? value, string? label)
    {
        var cursor = Resolve(document, start);

        if (cursor.Content == null || string.IsNullOrEmpty(value))
        {
            return null;
        }

        var runs = Runs(cursor.Content);
        var last = Resolve(document, end);
        var to = start < end && last.Content == cursor.Content ? last.Offset : cursor.Offset;

        if (!image && start < end && last.Content != cursor.Content)
        {
            var mark = new Mark(MarkKind.Link, null, value);

            foreach (var (content, from, until) in Targets(document, start, end, cursor, last))
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

    private MarkdownEditorUpdate? InsertMarkdown(Document document, int start, int end, string value)
    {
        var cursor = start < end ? DeleteRange(document, start, end) : Resolve(document, start);

        if (cursor == null)
        {
            return null;
        }

        var fragment = MarkdownParser.Parse(value);

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

    private MarkdownEditorUpdate? InsertTable(Document document, int start, string? value)
    {
        var parts = (value ?? "3x3").Split(['x', ',', ' '], StringSplitOptions.RemoveEmptyEntries);
        var rows = parts.Length > 0 && int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedRows) ? Math.Max(1, parsedRows) : 3;
        var columns = parts.Length > 1 && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedColumns) ? Math.Max(1, parsedColumns) : 3;
        var cursor = Resolve(document, start);
        var table = new Table();
        var header = new TableHeaderRow();

        for (var column = 0; column < columns; column++)
        {
            header.Add(string.Empty);
            header.Cells[column].Pristine = false;
        }

        table.InsertRow(0, header);

        for (var row = 1; row < rows; row++)
        {
            var added = new TableRow();

            for (var column = 0; column < columns; column++)
            {
                added.Add(string.Empty);
                added.Cells[column].Pristine = false;
            }

            table.InsertRow(row, added);
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

    private MarkdownEditorUpdate? TableCommand(Document document, string name, int caret, string? value)
    {
        var cursor = Resolve(document, caret);

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
                var added = new TableRow();

                for (var index = 0; index < columns; index++)
                {
                    added.Add(string.Empty, table.Rows[0].Cells[index].Alignment);
                    added.Cells[index].Pristine = false;
                }

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

    private MarkdownEditorUpdate DeleteTable(Document document, Table table)
    {
        var parent = table.Parent;
        var index = parent.IndexOf(table);
        var next = parent.Children.Skip(index + 1).FirstOrDefault(block => block is not Paragraph { Virtual: true });
        var previous = parent.Children.Take(index).LastOrDefault(block => block is not Paragraph { Virtual: true });
        parent.Remove(table);
        Touch(parent);

        if (next is Leaf nextLeaf and not (FencedCodeBlock or IndentedCodeBlock or HtmlBlock))
        {
            return Commit(document, nextLeaf, 0);
        }

        if (previous is Leaf previousLeaf and not (FencedCodeBlock or IndentedCodeBlock or HtmlBlock))
        {
            return CommitAtEnd(document, previousLeaf);
        }

        var replacement = new Paragraph();
        parent.Insert(previous != null ? parent.IndexOf(previous) + 1 : 0, replacement);
        Touch(replacement);
        return Commit(document, replacement, 0);
    }
}
