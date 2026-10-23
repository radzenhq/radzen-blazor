#nullable enable

namespace Radzen.Documents.Spreadsheet;

abstract class ReferenceFunction : FormulaFunction
{
    public abstract string? GetReferenceText(FunctionArguments arguments, out CellData? error);

    public override CellData Evaluate(FunctionArguments arguments) => CellData.FromError(CellError.Ref);
}
