#nullable enable

using System;

namespace Radzen.Blazor.Spreadsheet;

class RoundDownFunction : RoundFunctionBase
{
    public override string Name => "ROUNDDOWN";

    protected override double Round(double value) => Math.Floor(value);
}