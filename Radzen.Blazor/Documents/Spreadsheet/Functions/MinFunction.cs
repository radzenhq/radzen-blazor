#nullable enable

namespace Radzen.Documents.Spreadsheet;

class MinFunction : MinMaxBase
{
    public override string Name => "MIN";

    protected override CellData Compute(System.Collections.Generic.List<double> numbers) => AggregationMethods.Min(numbers);
}