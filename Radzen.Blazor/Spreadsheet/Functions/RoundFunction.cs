#nullable enable

using System;

namespace Radzen.Blazor.Spreadsheet;

class RoundFunction : RoundFunctionBase
{
    public override string Name => "ROUND";

    protected override double Round(double value)
    {
        return Math.Round(value, MidpointRounding.AwayFromZero);
    }
}


