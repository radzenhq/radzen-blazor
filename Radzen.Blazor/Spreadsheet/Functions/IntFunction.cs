#nullable enable

using System;

namespace Radzen.Blazor.Spreadsheet;

class IntFunction : FormulaFunction
{
    public override string Name => "INT";

    public override FunctionParameter[] Parameters =>
    [
        new("number", ParameterType.Single, isRequired: true)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var numberArg = arguments.GetSingle("number");

        if (numberArg == null)
        {
            return CellData.FromError(CellError.Value);
        }

        if (numberArg.IsError)
        {
            return numberArg;
        }

        if (!numberArg.TryCoerceToNumber(out var number, allowBooleans: false, nonNumericTextAsZero: false))
        {
            return CellData.FromError(CellError.Value);
        }

        var result = Math.Floor(number);

        return CellData.FromNumber(result);
    }
}