namespace Radzen.Documents;

internal readonly struct Nothing;

internal abstract class BlockVisitor<TContext, TResult>
{
    protected abstract TResult Default(Block block, TContext context);

    public virtual TResult Visit(Block block, TContext context) => Default(block, context);

    public virtual TResult Visit(Paragraph block, TContext context) => Default(block, context);

    public virtual TResult Visit(Table block, TContext context) => Default(block, context);

    public virtual TResult Visit(Image block, TContext context) => Default(block, context);

    public virtual TResult Visit(ListBlock block, TContext context) => Default(block, context);

    public virtual TResult Visit(Container block, TContext context) => Default(block, context);

    public virtual TResult Visit(PageBreak block, TContext context) => Default(block, context);

    public virtual TResult Visit(QrCode block, TContext context) => Default(block, context);

    public virtual TResult Visit(Barcode block, TContext context) => Default(block, context);

    public virtual TResult Visit(TableOfContents block, TContext context) => Default(block, context);
}
