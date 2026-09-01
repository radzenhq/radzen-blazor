using System;

namespace Radzen.Documents.Markdown;

/// <summary>
/// Represents a strikethrough node: <c>~~strikethrough~~</c>.
/// </summary>
public class Strikethrough : InlineContainer
{
    /// <inheritdoc />
    public override void Accept(INodeVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.VisitStrikethrough(this);
    }
}
