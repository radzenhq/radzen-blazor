namespace Radzen.Documents.Pdf.Render;

internal sealed class EmitContext
{
    public required PagePlan Plan { get; init; }
    public required TextLinePlanner Text { get; init; }
    public required CodeSymbolPlanner CodeSymbols { get; init; }
    public required ImagePlanner Images { get; init; }
    public required TablePlanner Tables { get; init; }
    public required BoxPlanner Boxes { get; init; }
}
