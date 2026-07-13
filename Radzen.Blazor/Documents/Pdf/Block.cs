using Radzen.Documents.Pdf.Emit;
namespace Radzen.Documents.Pdf;


/// <summary>
/// The abstract base of all block-level content (paragraphs, tables, images, page breaks).
/// </summary>
public abstract class Block
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Block"/> class.
    /// </summary>
    protected Block()
    {
    }

    // Double-dispatch entry point: routes to the visitor overload for this concrete type.
    internal abstract TResult Accept<TContext, TResult>(BlockVisitor<TContext, TResult> visitor, TContext context);
}
