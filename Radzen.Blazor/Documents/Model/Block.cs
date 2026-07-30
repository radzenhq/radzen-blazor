namespace Radzen.Documents;


/// <summary>
/// The abstract base of all block-level content (paragraphs, tables, images, page breaks).
/// The hierarchy is closed: the set of block kinds is fixed by this library and cannot be
/// extended from outside it.
/// </summary>
public abstract class Block
{
    internal Block()
    {
    }

    internal object? Owner { get; set; }

    internal virtual TResult Accept<TContext, TResult>(BlockVisitor<TContext, TResult> visitor, TContext context)
        => visitor.Visit(this, context);
}
