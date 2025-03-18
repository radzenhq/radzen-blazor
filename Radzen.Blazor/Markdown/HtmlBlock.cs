using System.Text.RegularExpressions;

namespace Radzen.Blazor.Markdown;

/// <summary>
/// Represents an HTML block.
/// </summary>
public class HtmlBlock : Leaf
{
    internal int Type { get; private set; }

    /// <inheritdoc />
    public override void Accept(INodeVisitor visitor)
    {
        visitor.VisitHtmlBlock(this);
    }

    internal override BlockMatch Matches(BlockParser parser)
    {
        return parser.Blank && (Type == 6 || Type == 7) ? BlockMatch.Skip : BlockMatch.Match;
    }

    private static readonly Regex TrailinNewLineRegex = new(@"\n$");

    internal override void Close(BlockParser parser)
    {
        base.Close(parser);

        Value = TrailinNewLineRegex.Replace(Value, "");
    }

    internal static BlockStart Start(BlockParser parser, Block node)
    {
        if (!parser.Indented && parser.PeekNonSpace() == '<')
        {
            var line = parser.CurrentLine[parser.NextNonSpace..];

            for (var blockType = 1; blockType <= 7; blockType++) {

                if (BlockParser.HtmlBlockOpenRegex[blockType].IsMatch(line) &&
                    (blockType < 7 || (node is not Paragraph && 
                    !(!parser.AllClosed && !parser.Blank && parser.Tip is Paragraph) // maybe lazy
                    ))) {
                    parser.CloseUnmatchedBlocks();
                    // We don't adjust parser.offset;
                    // spaces are part of the HTML block:
                    var block = parser.AddChild<HtmlBlock>(parser.Offset);
                    block.Type = blockType;
                    return BlockStart.Leaf;
                }
            }
        }

        return BlockStart.Skip;
    }
}