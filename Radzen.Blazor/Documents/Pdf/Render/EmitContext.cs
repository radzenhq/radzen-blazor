namespace Radzen.Documents.Pdf.Render;

internal sealed class EmitContext
{
    public required PagePlan Plan { get; init; }
    public required TextLinePlanner Text { get; init; }
    public required CodeSymbolPlanner CodeSymbols { get; init; }
}
