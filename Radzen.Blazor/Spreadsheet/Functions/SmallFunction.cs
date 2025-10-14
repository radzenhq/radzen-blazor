#nullable enable

using System.Collections.Generic;

namespace Radzen.Blazor.Spreadsheet;

class SmallFunction : KOrderFunctionBase
{
    public override string Name => "SMALL";

    protected override CellData Compute(List<double> numbers, int k) => AggregationMethods.Small(numbers, k);
}