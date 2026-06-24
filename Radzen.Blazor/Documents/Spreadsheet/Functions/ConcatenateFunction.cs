#nullable enable

namespace Radzen.Documents.Spreadsheet;

// CONCATENATE is the legacy name for CONCAT; shares the same implementation.
class ConcatenateFunction : ConcatFunction
{
    public override string Name => "CONCATENATE";
}
