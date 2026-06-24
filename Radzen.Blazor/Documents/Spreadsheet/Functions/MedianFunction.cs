#nullable enable

using System.Collections.Generic;

namespace Radzen.Documents.Spreadsheet;

class MedianFunction : StatisticalAggregateFunctionBase
{
    public override string Name => "MEDIAN";

    protected override CellData Compute(List<double> numbers) => AggregationMethods.Median(numbers);
}
