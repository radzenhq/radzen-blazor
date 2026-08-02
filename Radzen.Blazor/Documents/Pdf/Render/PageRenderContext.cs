namespace Radzen.Documents.Pdf.Render;

internal sealed class PageRenderContext(PagePlan plan, TextLineRecorder text)
{
    public PagePlan Plan { get; } = plan;
    public int Layer { get; set; }
    private PaintStackOrder? stackParent;

    public StackMark BeginStack(int zOrder)
    {
        var order = new PaintStackOrder(stackParent, zOrder);
        stackParent = order;
        return Plan.BeginStack(Layer, order);
    }

    public void EndStack(in StackMark mark)
    {
        Plan.EndStack(mark);
        stackParent = mark.Stack.Order.Parent;
    }
    public TextLineRecorder Text { get; } = text;
}
