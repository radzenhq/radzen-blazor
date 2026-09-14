using System;
using System.Globalization;

namespace Radzen.Documents.Markdown;

internal sealed class SourceMarker : NodeVisitorBase
{
    private readonly string source;

    public SourceMarker(string source)
    {
        this.source = source;
    }

    private static void Mark(Block block)
    {
        block.Pristine = true;
    }

    private char At(int offset) => offset >= 0 && offset < source.Length ? source[offset] : '\0';

    private int Run(Inline inline, char ch)
    {
        var count = 0;

        while (inline.SourceStart + count < inline.SourceEnd && At(inline.SourceStart + count) == ch)
        {
            count++;
        }

        return count;
    }

    public override void VisitDocument(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);
        Mark(document);
        base.VisitDocument(document);
    }

    public override void VisitBlockQuote(BlockQuote blockQuote)
    {
        ArgumentNullException.ThrowIfNull(blockQuote);
        Mark(blockQuote);
        base.VisitBlockQuote(blockQuote);
    }

    public override void VisitHeading(Heading heading)
    {
        ArgumentNullException.ThrowIfNull(heading);
        Mark(heading);

        if (heading is SetExtHeading setext)
        {
            setext.Underline = LastLine(heading);
        }

        base.VisitHeading(heading);
    }

    public override void VisitParagraph(Paragraph paragraph)
    {
        ArgumentNullException.ThrowIfNull(paragraph);
        Mark(paragraph);
        base.VisitParagraph(paragraph);
    }

    public override void VisitUnorderedList(UnorderedList unorderedList)
    {
        ArgumentNullException.ThrowIfNull(unorderedList);
        Mark(unorderedList);
        base.VisitUnorderedList(unorderedList);
    }

    public override void VisitOrderedList(OrderedList orderedList)
    {
        ArgumentNullException.ThrowIfNull(orderedList);
        Mark(orderedList);
        base.VisitOrderedList(orderedList);
    }

    public override void VisitListItem(ListItem listItem)
    {
        ArgumentNullException.ThrowIfNull(listItem);
        Mark(listItem);
        var digits = 0;

        while (char.IsDigit(At(listItem.SourceStart + digits)))
        {
            digits++;
        }

        listItem.Number = digits > 0 ? int.Parse(source.AsSpan(listItem.SourceStart, digits), CultureInfo.InvariantCulture) : null;
        base.VisitListItem(listItem);
    }

    public override void VisitThematicBreak(ThematicBreak thematicBreak)
    {
        ArgumentNullException.ThrowIfNull(thematicBreak);
        Mark(thematicBreak);
        thematicBreak.Line = Slice(thematicBreak.SourceStart, thematicBreak.SourceEnd).Trim();
    }

    private string Slice(int start, int end) => start >= 0 && end <= source.Length && end > start ? source[start..end] : string.Empty;

    private string LastLine(Block block)
    {
        var slice = Slice(block.SourceStart, block.SourceEnd);
        return slice[(slice.LastIndexOf('\n') + 1)..].TrimStart(' ', '\t', '>').TrimEnd();
    }

    public override void VisitIndentedCodeBlock(IndentedCodeBlock codeBlock)
    {
        ArgumentNullException.ThrowIfNull(codeBlock);
        Mark(codeBlock);
    }

    public override void VisitFencedCodeBlock(FencedCodeBlock fencedCodeBlock)
    {
        ArgumentNullException.ThrowIfNull(fencedCodeBlock);
        Mark(fencedCodeBlock);
    }

    public override void VisitHtmlBlock(HtmlBlock htmlBlock)
    {
        ArgumentNullException.ThrowIfNull(htmlBlock);
        Mark(htmlBlock);
    }

    public override void VisitTable(Table table)
    {
        ArgumentNullException.ThrowIfNull(table);
        Mark(table);
        var lines = Slice(table.SourceStart, table.SourceEnd).Split('\n');
        table.DelimiterLine = lines.Length > 1 ? lines[1].TrimStart(' ', '\t', '>') : null;
        base.VisitTable(table);
    }

    public override void VisitTableCell(TableCell cell)
    {
        ArgumentNullException.ThrowIfNull(cell);
        cell.Pristine = true;
        base.VisitTableCell(cell);
    }

    public override void VisitEmphasis(Emphasis emphasis)
    {
        ArgumentNullException.ThrowIfNull(emphasis);
        emphasis.Marker = At(emphasis.SourceStart) is '*' or '_' ? At(emphasis.SourceStart) : null;
        base.VisitEmphasis(emphasis);
    }

    public override void VisitStrong(Strong strong)
    {
        ArgumentNullException.ThrowIfNull(strong);
        strong.Marker = At(strong.SourceStart) is '*' or '_' ? At(strong.SourceStart) : null;
        base.VisitStrong(strong);
    }

    public override void VisitStrikethrough(Strikethrough strikethrough)
    {
        ArgumentNullException.ThrowIfNull(strikethrough);
        var tildes = Run(strikethrough, '~');
        strikethrough.Tildes = tildes > 0 ? tildes : null;
        base.VisitStrikethrough(strikethrough);
    }

    public override void VisitCode(Code code)
    {
        ArgumentNullException.ThrowIfNull(code);
        var ticks = Run(code, '`');
        code.Ticks = ticks > 0 ? ticks : null;
    }

    public override void VisitLink(Link link)
    {
        ArgumentNullException.ThrowIfNull(link);
        link.Autolink = At(link.SourceStart) == '<';
        link.Suffix = Suffix(link);
        base.VisitLink(link);
    }

    public override void VisitImage(Image image)
    {
        ArgumentNullException.ThrowIfNull(image);
        image.Suffix = Suffix(image);
        base.VisitImage(image);
    }

    private string? Suffix(InlineContainer link)
    {
        if (link.SourceEnd > source.Length || link.SourceEnd <= link.SourceStart || source[link.SourceEnd - 1] != ']')
        {
            return null;
        }

        var contentEnd = link.Children.Count > 0 ? link.Children[^1].SourceEnd : link.SourceStart + (link is Image ? 2 : 1);
        return contentEnd <= link.SourceEnd ? source[contentEnd..link.SourceEnd] : null;
    }

    public override void VisitLineBreak(LineBreak lineBreak)
    {
        ArgumentNullException.ThrowIfNull(lineBreak);
        lineBreak.Backslash = At(lineBreak.SourceStart) == '\\';
    }
}
