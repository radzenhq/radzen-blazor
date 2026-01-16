#nullable enable

using System.Security.Cryptography;

namespace Radzen.Blazor.Spreadsheet;

class RandFunction : FormulaFunction
{
    public override string Name => "RAND";

    public override FunctionParameter[] Parameters =>
    [
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var value = RandomNumberGenerator.GetInt32(int.MaxValue) / (double)int.MaxValue;
        return CellData.FromNumber(value);
    }
}