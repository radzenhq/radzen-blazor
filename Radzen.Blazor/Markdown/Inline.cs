namespace Radzen.Blazor.Markdown;

/// <summary>
/// Base class for markdown inline nodes.
/// </summary>
public abstract class Inline : INode
{
    /// <summary>
    /// Accepts a visitor.
    /// </summary>
    public abstract void Accept(INodeVisitor visitor);
}
