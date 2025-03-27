using System.Text.RegularExpressions;

namespace Radzen.Blazor.Markdown;

/// <summary>
/// Represents a thematic break node: <c>***</c>, <c>---</c>, or <c>___</c>.
/// </summary>
public class ThematicBreak : Block
{
    private static readonly Regex ThematicBreakRegex = new (@"^(?:\*[ \t]*){3,}$|^(?:_[ \t]*){3,}$|^(?:-[ \t]*){3,}$");

    /// <inheritdoc />
    public override void Accept(INodeVisitor visitor)
    {
        visitor.VisitThematicBreak(this);
    }

    internal override BlockMatch Matches(BlockParser parser)
    {
        // A thematic break can never contain other blocks
        return BlockMatch.Skip;
    }

    internal static BlockStart Start(BlockParser parser, Block container)
    {
        if (!parser.Indented)
        {
            var line = parser.CurrentLine[parser.NextNonSpace..];

            if (ThematicBreakRegex.IsMatch(line))
            {
                parser.CloseUnmatchedBlocks();
                parser.AddChild<ThematicBreak>(parser.NextNonSpace);
                parser.AdvanceOffset(parser.CurrentLine.Length - parser.Offset, false);
                return BlockStart.Leaf;
            }
        }

        return BlockStart.Skip;
    }
}