#nullable enable

namespace Radzen.Blazor.Spreadsheet;

class ErrorFunction : FormulaFunction
{
    public override FunctionParameter[] Parameters => [];

    public override CellData Evaluate(FunctionArguments arguments) => CellData.FromError(CellError.Name);
}