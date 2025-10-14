#nullable enable

using System;

namespace Radzen.Blazor.Spreadsheet;

abstract class RoundFunctionBase : FormulaFunction
{
    public override FunctionParameter[] Parameters =>
    [
        new("number", ParameterType.Single, isRequired: true),
        new("num_digits", ParameterType.Single, isRequired: true)
    ];

    protected abstract double Round(double value);

    protected virtual bool TryGetDefaultDigits(out int digits)
    {
        digits = 0;
        return false;
    }

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var numberArg = arguments.GetSingle("number");
        var digitsArg = arguments.GetSingle("num_digits");

        if (numberArg == null)
        {
            return CellData.FromError(CellError.Value);
        }
        
        if (numberArg.IsError)
        {
            return numberArg;
        }

        if (digitsArg != null && digitsArg.IsError)
        {
            return digitsArg;
        }

        if (!numberArg.TryCoerceToNumber(out var number, allowBooleans: false, nonNumericTextAsZero: false))
        {
            return CellData.FromError(CellError.Value);
        }

        double? digits = null;

        if (digitsArg == null)
        {
            if (TryGetDefaultDigits(out var def))
            {
                digits = def;
            }
            else
            {
                return CellData.FromError(CellError.Value);
            }
        }
        else if (digitsArg.Type == CellDataType.Number)
        {
            digits = digitsArg.GetValueOrDefault<double?>();
        }
        else if (digitsArg.Type == CellDataType.String)
        {
            if (digitsArg.TryCoerceToNumber(out var dn, allowBooleans: false, nonNumericTextAsZero: false))
            {
                digits = dn;
            }
        }

        if (digits is null)
        {
            return CellData.FromError(CellError.Value);
        }

        var n = number;
        var d = (int)Math.Truncate(digits.Value);

        double result;

        if (d >= 0)
        {
            var factor = Math.Pow(10, d);
            var rounded = Round(Math.Abs(n) * factor);
            result = Math.Sign(n) * rounded / factor;
        }
        else
        {
            var factor = Math.Pow(10, -d);
            var rounded = Round(Math.Abs(n) / factor);
            result = Math.Sign(n) * rounded * factor;
        }

        return CellData.FromNumber(result);
    }
}