namespace Radzen.Documents.Pdf.Emit;

// The result type of a visitor that runs purely for its side effects.
internal readonly struct Nothing;

// Double-dispatch over the concrete Block types: the single polymorphic contract that
// replaces the is/switch chains previously replicated across the paginator, layout,
// style/opacity resolvers, structure builder and emitters. A dispatch site subclasses
// this, overrides only the block kinds it treats specially, and routes the rest through
// Default - which either fails loud or returns an identity result exactly as the switch's
// default arm did. Adding a Block type means adding a Visit overload here and one Accept
// override on that type; the dispatch sites that already fall through Default are unaffected.
internal abstract class BlockVisitor<TContext, TResult>
{
    protected abstract TResult Default(Block block, TContext context);

    // Catch-all for a Block type with no dedicated overload (only reachable from a Block
    // subclass outside the known set, e.g. a test double); routes to the same Default arm
    // the switches used to fall through, so an unmapped type still fails loud where it must.
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
