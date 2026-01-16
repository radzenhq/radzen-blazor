#nullable enable

using System.Security.Cryptography;

namespace Radzen.Blazor.Spreadsheet;

class RandBetweenFunction : FormulaFunction
{
    public override string Name => "RANDBETWEEN";

    public override FunctionParameter[] Parameters =>
    [
        new("bottom", ParameterType.Single, isRequired: true),
        new("top", ParameterType.Single, isRequired: true)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        if (!TryGetInteger(arguments, "bottom", isRequired: true, defaultValue: null, out var bottom, out var error))
        {
            return error!;
        }

        if (!TryGetInteger(arguments, "top", isRequired: true, defaultValue: null, out var top, out error))
        {
            return error!;
        }

        if (bottom > top)
        {
            return CellData.FromError(CellError.Num);
        }

        if (bottom == top)
        {
            return CellData.FromNumber(bottom);
        }

        var range = (long)top - bottom + 1;
        if (range <= 0 || range > int.MaxValue)
        {
            return CellData.FromError(CellError.Num);
        }

        var result = bottom + RandomNumberGenerator.GetInt32((int)range);
        return CellData.FromNumber(result);
    }
}