#nullable enable

using System;

namespace Radzen.Documents.Spreadsheet;

class SqrtFunction : UnaryMathFunctionBase
{
    public override string Name => "SQRT";

    protected override CellData Compute(double number)
    {
        return number < 0 ? CellData.FromError(CellError.Num) : CellData.FromNumber(Math.Sqrt(number));
    }
}
