#nullable enable

using System;

namespace Radzen.Documents.Spreadsheet;

class CeilingFunction : FormulaFunction
{
    public override string Name => "CEILING";

    public override FunctionParameter[] Parameters =>
    [
        new("number", ParameterType.Single, isRequired: true),
        new("significance", ParameterType.Single, isRequired: true)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        if (!TryGetNumber(arguments, "number", isRequired: true, defaultValue: null, out var number, out var numberError))
        {
            return numberError!;
        }

        if (!TryGetNumber(arguments, "significance", isRequired: true, defaultValue: null, out var significance, out var significanceError))
        {
            return significanceError!;
        }

        // Legacy CEILING returns 0 for zero significance (verified against Excel) - unlike FLOOR (#DIV/0!).
        if (significance == 0)
        {
            return CellData.FromNumber(0);
        }

        if (number > 0 && significance < 0)
        {
            return CellData.FromError(CellError.Num);
        }

        // Rounds away from zero to the nearest multiple of significance.
        return CellData.FromNumber(Math.Ceiling(number / significance) * significance);
    }
}
