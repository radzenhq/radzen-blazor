#nullable enable

namespace Radzen.Blazor.Spreadsheet;

class MaxFunction : MinMaxBase
{
    public override string Name => "MAX";
    protected override CellData Compute(System.Collections.Generic.List<double> numbers) => AggregationMethods.Max(numbers);
}