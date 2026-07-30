namespace Radzen.Documents;


/// <summary>
/// A block that forces the following content onto a new page.
/// </summary>
public sealed class PageBreak : Block
{
    internal override TResult Accept<TContext, TResult>(BlockVisitor<TContext, TResult> visitor, TContext context) => visitor.Visit(this, context);
}
