namespace Radzen.Documents.Pdf;

// Per-page emission context: the PagePlan being filled, this page's number/count, and
// the shared element emitters so any emitter can dispatch to a sibling (a table cell
// emits lines, a barcode emits its caption, the body emits fields).
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
