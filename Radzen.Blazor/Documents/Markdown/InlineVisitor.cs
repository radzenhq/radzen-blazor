using System.Collections.Generic;

namespace Radzen.Documents.Markdown;

class InlineVisitor(Dictionary<string, LinkReference> references) : NodeVisitorBase
{
    public override void VisitHeading(Heading heading) => ParseChildren(heading, references);

    private static void ParseChildren(IBlockInlineContainer node, Dictionary<string, LinkReference> references)
    {
        var inlines = InlineParser.Parse(node.Value, references);

        if (node is Leaf leaf)
        {
            InlineParser.Locate(inlines, leaf.Content);
        }
        else if (node is TableCell cell)
        {
            InlineParser.Locate(inlines, cell.Content);
        }

        foreach (var inline in inlines)
        {
            node.Add(inline);
        }
    }

    public override void VisitParagraph(Paragraph paragraph) => ParseChildren(paragraph, references);

    public override void VisitTableCell(TableCell cell) => ParseChildren(cell, references);
}