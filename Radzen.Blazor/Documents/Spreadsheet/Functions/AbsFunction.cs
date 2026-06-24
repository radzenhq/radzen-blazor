#nullable enable

using System;

namespace Radzen.Documents.Spreadsheet;

class AbsFunction : UnaryMathFunctionBase
{
    public override string Name => "ABS";

    protected override CellData Compute(double number) => CellData.FromNumber(Math.Abs(number));
}
