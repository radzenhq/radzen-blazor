
namespace Radzen.Blazor.Markdown;

/// <summary>
/// A base class for all heading elements.
/// </summary>
public abstract class Heading : Leaf
{
    /// <summary>
    /// The level of the heading. The value is between 1 and 6.
    /// </summary>
    public int Level { get; set; }

    /// <inheritdoc />
    public override void Accept(INodeVisitor visitor)
    {
        visitor.VisitHeading(this);
    }

    internal override BlockMatch Matches(BlockParser parser)
    {
        // a heading can never container another line
        return BlockMatch.Skip;
    }
}