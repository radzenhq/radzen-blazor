
namespace Radzen.Blazor.Markdown;

/// <summary>
/// Represents a paragraph node.
/// </summary>
public class Paragraph : Leaf
{
    /// <inheritdoc />
    public override void Accept(INodeVisitor visitor)
    {
        visitor.VisitParagraph(this);
    }

    internal override BlockMatch Matches(BlockParser parser)
    {
        return parser.Blank ? BlockMatch.Skip : BlockMatch.Match;
    }
}