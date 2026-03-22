#nullable enable

namespace Radzen.Documents.Spreadsheet;

class MaxAllFunction : MinMaxAllBase
{
    public override string Name => "MAXA";
    protected override CellData Compute(System.Collections.Generic.List<double> numbers) => AggregationMethods.Max(numbers);
}