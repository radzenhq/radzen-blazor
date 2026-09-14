using System;
using System.Linq;

namespace Radzen.Documents.Markdown;

internal static class DocumentCloner
{
    public static Document Clone(Document document)
    {
        var copy = new Document();
        copy.LinkReferenceDefinitions.AddRange(document.LinkReferenceDefinitions);
        Fill(document, copy);
        return copy;
    }

    private static void Fill(BlockContainer from, BlockContainer to)
    {
        foreach (var child in from.Children)
        {
            to.Add(Clone(child));
        }
    }

    private static Block Clone(Block block)
    {
        Block copy = block switch
        {
            Paragraph paragraph => new Paragraph { Virtual = paragraph.Virtual },
            SetExtHeading setext => new SetExtHeading { Level = setext.Level, Underline = setext.Underline },
            Heading heading => new AtxHeading { Level = heading.Level },
            ThematicBreak rule => new ThematicBreak { Line = rule.Line },
            FencedCodeBlock fenced => new FencedCodeBlock { Value = fenced.Value, Info = fenced.Info, Delimiter = fenced.Delimiter, Closed = fenced.Closed, Indent = fenced.Indent },
            IndentedCodeBlock indented => new IndentedCodeBlock { Value = indented.Value },
            HtmlBlock html => new HtmlBlock { Value = html.Value, Type = html.Type },
            Table table => CloneTable(table),
            ListItem item => new ListItem { Checked = item.Checked, Number = item.Number, ContentOffset = item.ContentOffset },
            OrderedList ordered => new OrderedList { Start = ordered.Start, Delimiter = ordered.Delimiter, Marker = ordered.Marker, Padding = ordered.Padding, Tight = ordered.Tight, MarkerOffset = ordered.MarkerOffset },
            UnorderedList unordered => new UnorderedList { Marker = unordered.Marker, Padding = unordered.Padding, Tight = unordered.Tight, MarkerOffset = unordered.MarkerOffset },
            BlockQuote => new BlockQuote(),
            _ => throw new NotSupportedException(block.GetType().Name)
        };

        copy.SourceStart = block.SourceStart;
        copy.SourceEnd = block.SourceEnd;
        copy.Pristine = block.Pristine;

        if (block is Leaf leaf && copy is Leaf leafCopy)
        {
            leafCopy.Content.Append(leaf.Content.Segments);
            leafCopy.ReplaceInlines(leaf.Children.Select(Clone));
        }

        if (block is BlockContainer container && copy is BlockContainer containerCopy)
        {
            Fill(container, containerCopy);
        }

        return copy;
    }

    private static Table CloneTable(Table table)
    {
        var copy = new Table { DelimiterLine = table.DelimiterLine };

        foreach (var row in table.Rows)
        {
            var rowCopy = row is TableHeaderRow ? new TableHeaderRow() : new TableRow();

            foreach (var cell in row.Cells)
            {
                rowCopy.Add(cell.Value, cell.Alignment, cell.Content);
                rowCopy.Cells[^1].Pristine = cell.Pristine;
                rowCopy.Cells[^1].ReplaceInlines(cell.Children.Select(Clone));
            }

            copy.InsertRow(copy.Rows.Count, rowCopy);
        }

        return copy;
    }

    public static Inline Clone(Inline inline)
    {
        Inline copy = inline switch
        {
            Text text => new Text(text.Value),
            Code code => new Code(code.Value) { Ticks = code.Ticks },
            Emphasis emphasis => Fill(new Emphasis { Marker = emphasis.Marker }, emphasis),
            Strong strong => Fill(new Strong { Marker = strong.Marker }, strong),
            Strikethrough strikethrough => Fill(new Strikethrough { Tildes = strikethrough.Tildes }, strikethrough),
            Link link => Fill(new Link { Destination = link.Destination, Title = link.Title, Suffix = link.Suffix, Autolink = link.Autolink }, link),
            Image image => Fill(new Image { Destination = image.Destination, Title = image.Title, Suffix = image.Suffix }, image),
            HtmlInline html => new HtmlInline { Value = html.Value },
            LineBreak lineBreak => new LineBreak { Backslash = lineBreak.Backslash },
            _ => new SoftLineBreak()
        };

        copy.SourceStart = inline.SourceStart;
        copy.SourceEnd = inline.SourceEnd;
        return copy;
    }

    private static InlineContainer Fill(InlineContainer container, InlineContainer source)
    {
        foreach (var child in source.Children)
        {
            container.Add(Clone(child));
        }

        return container;
    }
}
