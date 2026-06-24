#nullable enable

using System;

namespace Radzen.Documents.Spreadsheet;

class PowerFunction : FormulaFunction
{
    public override string Name => "POWER";

    public override FunctionParameter[] Parameters =>
    [
        new("number", ParameterType.Single, isRequired: true),
        new("power", ParameterType.Single, isRequired: true)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        if (!TryGetNumber(arguments, "number", isRequired: true, defaultValue: null, out var number, out var numberError))
        {
            return numberError!;
        }

        if (!TryGetNumber(arguments, "power", isRequired: true, defaultValue: null, out var power, out var powerError))
        {
            return powerError!;
        }

        var result = Math.Pow(number, power);

        if (double.IsNaN(result))
        {
            return CellData.FromError(CellError.Num);
        }

        if (double.IsInfinity(result))
        {
            return CellData.FromError(CellError.Div0);
        }

        return CellData.FromNumber(result);
    }
}
