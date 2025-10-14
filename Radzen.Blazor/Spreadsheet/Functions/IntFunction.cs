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

        double? number = null;

        if (numberArg.Type == CellDataType.Number)
        {
            number = numberArg.GetValueOrDefault<double>();
        }
        else if (numberArg.Type == CellDataType.String)
        {
            if (CellData.TryConvertFromString(numberArg.GetValueOrDefault<string>(), out var converted, out var valueType) && valueType == CellDataType.Number)
            {
                number = (double)converted!;
            }
        }

        if (number is null)
        {
            return CellData.FromError(CellError.Value);
        }

        var n = number.Value;
        var result = Math.Floor(n);

        return CellData.FromNumber(result);
    }
}