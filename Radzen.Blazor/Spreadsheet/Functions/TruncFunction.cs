#nullable enable

using System;

namespace Radzen.Blazor.Spreadsheet;

class TruncFunction : RoundFunctionBase
{
    public override string Name => "TRUNC";

    public override FunctionParameter[] Parameters =>
    [
        new("number", ParameterType.Single, isRequired: true),
        new("num_digits", ParameterType.Single, isRequired: false)
    ];

    protected override bool TryGetDefaultDigits(out int digits)
    {
        digits = 0;
        return true;
    }

    protected override double Round(double value)
    {
        return Math.Truncate(value);
    }
}