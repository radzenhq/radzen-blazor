using System.Collections.Generic;

namespace Radzen.Blazor.Markdown;

class InlineVisitor(Dictionary<string, LinkReference> references) : NodeVisitorBase
{
    public override void VisitHeading(Heading heading) => ParseChildren(heading, references);

    private static void ParseChildren(IBlockInlineContainer node, Dictionary<string, LinkReference> references)
    {
        var inlines = InlineParser.Parse(node.Value, references);

        foreach (var inline in inlines)
        {
            node.Add(inline);
        }
    }

    public override void VisitParagraph(Paragraph paragraph) => ParseChildren(paragraph, references);

    public override void VisitTableCell(TableCell cell) => ParseChildren(cell, references);
}