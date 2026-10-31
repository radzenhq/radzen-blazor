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

        return Calculate(number, power);
    }

    internal static CellData Calculate(double number, double power)
    {
        if (number == 0d && power == 0d)
        {
            return CellData.FromError(CellError.Num);
        }

        if (number == 0d && power < 0d)
        {
            return CellData.FromError(CellError.Div0);
        }

        var result = number < 0d && IsOddRoot(power) ? -Math.Pow(-number, power) : Math.Pow(number, power);

        return double.IsNaN(result) || double.IsInfinity(result)
            ? CellData.FromError(CellError.Num)
            : CellData.FromNumber(result);
    }

    private static bool IsOddRoot(double power)
    {
        const double Tolerance = 1e-14;

        var root = 1d / power;
        var nearest = Math.Round(root);

        return Math.Abs(root - nearest) < Tolerance && nearest % 2d != 0d;
    }
}
