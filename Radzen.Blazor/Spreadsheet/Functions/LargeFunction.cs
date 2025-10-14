#nullable enable

using System.Collections.Generic;

namespace Radzen.Blazor.Spreadsheet;

class LargeFunction : KOrderFunctionBase
{
    public override string Name => "LARGE";

    protected override CellData Compute(List<double> numbers, int k) => AggregationMethods.Large(numbers, k);
}