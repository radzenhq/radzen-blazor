#nullable enable

namespace Radzen.Documents.Spreadsheet;

class ErrorFunction : FormulaFunction
{
    public override string Name => "ERROR";

    public override FunctionParameter[] Parameters => [];

    public override CellData Evaluate(FunctionArguments arguments) => CellData.FromError(CellError.Name);
}