namespace Radzen.Blazor.Markdown;


/// <summary>
/// Represents a markdown block quote: <c>&gt; Quote</c>.
/// </summary>
public class BlockQuote : BlockContainer
{
    /// <inheritdoc />
    public override void Accept(INodeVisitor visitor)
    {
        visitor.VisitBlockQuote(this);
    }

    /// <inheritdoc />
    public override bool CanContain(Block node)
    {
        return node is not ListItem;
    }

    internal override BlockMatch Matches(BlockParser parser)
    {
        if (!parser.Indented && parser.PeekNonSpace() == '>')
        {
            parser.AdvanceNextNonSpace();
            parser.AdvanceOffset(1, false);
            // optional following space

            if (parser.Peek().IsSpaceOrTab())
            {
                parser.AdvanceOffset(1, true);
            }
        }
        else
        {
            return BlockMatch.Skip;
        }

        return BlockMatch.Match;
    }

    internal static BlockStart Start(BlockParser parser, Block block)
    {
        if (!parser.Indented && parser.PeekNonSpace() == '>') 
        {
            parser.AdvanceNextNonSpace();
            parser.AdvanceOffset(1, false);
            // optional following space

            if (parser.Peek().IsSpaceOrTab())
            {
                parser.AdvanceOffset(1, true);
            }

            parser.CloseUnmatchedBlocks();

            parser.AddChild<BlockQuote>(parser.NextNonSpace);

            return BlockStart.Container;
        }

        return BlockStart.Skip;
    }
}