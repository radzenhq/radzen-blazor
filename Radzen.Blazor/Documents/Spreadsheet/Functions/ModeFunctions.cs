#nullable enable

using System.Collections.Generic;

namespace Radzen.Documents.Spreadsheet;

class ModeFunction : StatisticalAggregateFunctionBase
{
    public override string Name => "MODE";

    protected override CellData Compute(List<double> numbers) => AggregationMethods.Mode(numbers);
}
