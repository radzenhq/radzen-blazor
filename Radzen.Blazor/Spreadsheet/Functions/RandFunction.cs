#nullable enable

namespace Radzen.Blazor.Spreadsheet;

class RandFunction : FormulaFunction
{
    public override string Name => "RAND";

    public override FunctionParameter[] Parameters =>
    [
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        return CellData.FromNumber(System.Random.Shared.NextDouble());
    }
}