using System;

namespace Radzen.Documents.Markdown;

internal sealed class PristineMarker : NodeVisitorBase
{
    private void Mark(Block block)
    {
        block.Pristine = true;
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
        base.VisitListItem(listItem);
    }

    public override void VisitThematicBreak(ThematicBreak thematicBreak)
    {
        ArgumentNullException.ThrowIfNull(thematicBreak);
        Mark(thematicBreak);
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
        base.VisitTable(table);
    }

    public override void VisitTableCell(TableCell cell)
    {
        ArgumentNullException.ThrowIfNull(cell);
        cell.Pristine = true;
        base.VisitTableCell(cell);
    }

    public override void VisitText(Text text)
    {
        ArgumentNullException.ThrowIfNull(text);
        text.Pristine = true;
    }

    public override void VisitEmphasis(Emphasis emphasis)
    {
        ArgumentNullException.ThrowIfNull(emphasis);
        emphasis.Pristine = true;
        base.VisitEmphasis(emphasis);
    }

    public override void VisitStrong(Strong strong)
    {
        ArgumentNullException.ThrowIfNull(strong);
        strong.Pristine = true;
        base.VisitStrong(strong);
    }

    public override void VisitStrikethrough(Strikethrough strikethrough)
    {
        ArgumentNullException.ThrowIfNull(strikethrough);
        strikethrough.Pristine = true;
        base.VisitStrikethrough(strikethrough);
    }

    public override void VisitCode(Code code)
    {
        ArgumentNullException.ThrowIfNull(code);
        code.Pristine = true;
    }

    public override void VisitLink(Link link)
    {
        ArgumentNullException.ThrowIfNull(link);
        link.Pristine = true;
        base.VisitLink(link);
    }

    public override void VisitImage(Image image)
    {
        ArgumentNullException.ThrowIfNull(image);
        image.Pristine = true;
        base.VisitImage(image);
    }

    public override void VisitHtmlInline(HtmlInline html)
    {
        ArgumentNullException.ThrowIfNull(html);
        html.Pristine = true;
    }

    public override void VisitLineBreak(LineBreak lineBreak)
    {
        ArgumentNullException.ThrowIfNull(lineBreak);
        lineBreak.Pristine = true;
    }

    public override void VisitSoftLineBreak(SoftLineBreak softLineBreak)
    {
        ArgumentNullException.ThrowIfNull(softLineBreak);
        softLineBreak.Pristine = true;
    }
}
