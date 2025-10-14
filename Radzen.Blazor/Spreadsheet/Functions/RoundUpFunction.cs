#nullable enable

using System;

namespace Radzen.Blazor.Spreadsheet;

class RoundUpFunction : RoundFunctionBase
{
    public override string Name => "ROUNDUP";

    protected override double Round(double value) => Math.Ceiling(value);
}