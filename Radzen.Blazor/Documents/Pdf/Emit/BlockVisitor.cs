namespace Radzen.Documents.Pdf.Emit;

// The result type of a visitor that runs purely for its side effects.
internal readonly struct Nothing;

// Adding a Block type means adding a Visit overload here and an Accept override on that
// type; dispatch sites that fall through Default are unaffected.
internal abstract class BlockVisitor<TContext, TResult>
{
    protected abstract TResult Default(Block block, TContext context);

    // Only reachable from a Block subclass outside the known set, e.g. a test double.
    public virtual TResult Visit(Block block, TContext context) => Default(block, context);

    public virtual TResult Visit(Paragraph block, TContext context) => Default(block, context);

    public virtual TResult Visit(Table block, TContext context) => Default(block, context);

    public virtual TResult Visit(Image block, TContext context) => Default(block, context);

    public virtual TResult Visit(List block, TContext context) => Default(block, context);

    public virtual TResult Visit(Container block, TContext context) => Default(block, context);

    public virtual TResult Visit(PageBreak block, TContext context) => Default(block, context);

    public virtual TResult Visit(QrCode block, TContext context) => Default(block, context);

    public virtual TResult Visit(Barcode block, TContext context) => Default(block, context);

    public virtual TResult Visit(TableOfContents block, TContext context) => Default(block, context);
}
