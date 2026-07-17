namespace Radzen.Documents.Pdf.Emit;

internal sealed class EmitContext
{
    public required PagePlan Plan { get; init; }
    public required int PageNumber { get; init; }
    public required int PageCount { get; init; }
    public required TextLineEmitter Text { get; init; }
    public required TableEmitter Tables { get; init; }
    public required ImageEmitter Images { get; init; }
    public required CodeEmitter Codes { get; init; }
    public required FieldResolver Fields { get; init; }
}
