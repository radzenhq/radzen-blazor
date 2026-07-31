namespace Radzen.Documents.Pdf.Render;

internal sealed class PageRenderContext(
    PagePlan plan,
    TextLineRecorder text,
    CodeSymbolRecorder codeSymbols,
    ImageRecorder images,
    TableRecorder tables,
    BoxRecorder boxes)
{
    public PagePlan Plan { get; } = plan;
    public TextLineRecorder Text { get; } = text;
    public CodeSymbolRecorder CodeSymbols { get; } = codeSymbols;
    public ImageRecorder Images { get; } = images;
    public TableRecorder Tables { get; } = tables;
    public BoxRecorder Boxes { get; } = boxes;
}
