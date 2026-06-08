#nullable enable

using System.Collections.Generic;

namespace Radzen.Documents.Spreadsheet;

class StdevFunction : StatisticalAggregateFunctionBase
{
    public override string Name => "STDEV";

    protected override CellData Compute(List<double> numbers) => AggregationMethods.StdevS(numbers);
}

class StdevpFunction : StatisticalAggregateFunctionBase
{
    public override string Name => "STDEVP";

    protected override CellData Compute(List<double> numbers) => AggregationMethods.StdevP(numbers);
}
