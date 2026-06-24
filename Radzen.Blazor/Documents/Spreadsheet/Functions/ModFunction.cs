#nullable enable

using System;

namespace Radzen.Documents.Spreadsheet;

class ModFunction : FormulaFunction
{
    public override string Name => "MOD";

    public override FunctionParameter[] Parameters =>
    [
        new("number", ParameterType.Single, isRequired: true),
        new("divisor", ParameterType.Single, isRequired: true)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        if (!TryGetNumber(arguments, "number", isRequired: true, defaultValue: null, out var number, out var numberError))
        {
            return numberError!;
        }

        if (!TryGetNumber(arguments, "divisor", isRequired: true, defaultValue: null, out var divisor, out var divisorError))
        {
            return divisorError!;
        }

        if (divisor == 0)
        {
            return CellData.FromError(CellError.Div0);
        }

        // Excel MOD result takes the sign of the divisor: n - divisor*INT(n/divisor), INT floors toward -inf.
        return CellData.FromNumber(number - divisor * Math.Floor(number / divisor));
    }
}
