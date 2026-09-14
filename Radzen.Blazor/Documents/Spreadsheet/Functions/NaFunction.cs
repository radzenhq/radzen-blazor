#nullable enable

namespace Radzen.Documents.Spreadsheet;

class NaFunction : FormulaFunction
{
    public override string Name => "NA";

    public override FunctionParameter[] Parameters => [];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        return CellData.FromError(CellError.NA);
    }
}
