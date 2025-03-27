using System.Collections.Generic;

namespace Radzen.Blazor.Markdown;

class LinkReferenceParser(BlockParser parser) : NodeVisitorBase
{
    private readonly List<Block> emptyNodes = [];

    public override void VisitParagraph(Paragraph paragraph)
    {
        var hasReferenceDefs = false;
        // Try parsing the beginning as link reference definitions;
        // Note that link reference definitions must be the beginning of a
        // paragraph node since link reference definitions cannot interrupt
        // paragraphs.
        while (paragraph.Value.Peek() == '[' && parser.TryParseLinkReference(paragraph.Value, out var position))
        {
            var removedText = paragraph.Value[..position];

            paragraph.Value = paragraph.Value[position..];
            hasReferenceDefs = true;

            var lines = removedText.Split('\n');

            // -1 for final newline.
            paragraph.Range.Start.Line += lines.Length - 1;
        }

        if (hasReferenceDefs && string.IsNullOrWhiteSpace(paragraph.Value))
        {
            emptyNodes.Add(paragraph);
        }
    }

    public static void Parse(BlockParser parser, Document document)
    {
        var visitor = new LinkReferenceParser(parser);

        document.Accept(visitor);

        foreach (var node in visitor.emptyNodes)
        {
            node.Remove();
        }
    }
}
