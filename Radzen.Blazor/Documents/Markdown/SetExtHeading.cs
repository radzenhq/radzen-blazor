using System.Text.RegularExpressions;

namespace Radzen.Documents.Markdown;

/// <summary>
/// Represents a setext heading node. Setext headings are headings that are underlined with equal signs for level 1 headings and dashes for level 2 headings.
/// </summary>
public class SetExtHeading : Heading
{
    internal string? Underline { get; set; }

    private static readonly Regex HeadingRegex = new (@"^(?:=+|-+)[ \t]*$");

    internal static BlockStart Start(BlockParser parser, Block block)
    {
        if (parser.Indented || block is not Paragraph paragraph)
        {
            return BlockStart.Skip;
        }

        var line = parser.CurrentLine[parser.NextNonSpace..];

        var match = HeadingRegex.Match(line);

        if (match.Success)
        {
            parser.CloseUnmatchedBlocks();

            // resolve reference links
            while (paragraph.Value.Peek() == '[' && parser.TryParseLinkReference(paragraph.Value,  out var position))
            {
                parser.RecordLinkReferenceDefinition(paragraph.Value[..position]);
                paragraph.TrimContentStart(position);
            }

            if (paragraph.Value.Length > 0) 
            {
                var heading = new SetExtHeading
                {
                    Level = match.Value[0] == '=' ? 1 : 2
                };
                heading.CopyContent(paragraph);
                paragraph.Parent.Replace(paragraph, heading);
                parser.Tip = heading;
                parser.AdvanceOffset(parser.CurrentLine.Length - parser.Offset, false);

                return BlockStart.Leaf;
            }
        }

        return BlockStart.Skip;
    }
}
