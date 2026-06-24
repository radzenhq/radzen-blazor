#nullable enable

using System;

namespace Radzen.Documents.Spreadsheet;

class FloorFunction : FormulaFunction
{
    public override string Name => "FLOOR";

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

        if (significance == 0)
        {
            return CellData.FromError(CellError.Div0);
        }

        if (number > 0 && significance < 0)
        {
            return CellData.FromError(CellError.Num);
        }

        // Rounds toward zero to the nearest multiple of significance (down for positives).
        return CellData.FromNumber(Math.Floor(number / significance) * significance);
    }
}
