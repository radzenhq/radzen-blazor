#nullable enable

namespace Radzen.Documents.Spreadsheet;

class MinAllFunction : MinMaxAllBase
{
    public override string Name => "MINA";
    protected override CellData Compute(System.Collections.Generic.List<double> numbers) => AggregationMethods.Min(numbers);
}