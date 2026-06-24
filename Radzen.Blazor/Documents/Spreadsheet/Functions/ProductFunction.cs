#nullable enable

using System.Collections.Generic;

namespace Radzen.Documents.Spreadsheet;

class ProductFunction : StatisticalAggregateFunctionBase
{
    public override string Name => "PRODUCT";

    protected override CellData Compute(List<double> numbers) => AggregationMethods.Product(numbers);
}
