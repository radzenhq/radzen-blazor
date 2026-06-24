#nullable enable

using System.Collections.Generic;

namespace Radzen.Documents.Spreadsheet;

class AverageIfFunction : FormulaFunction
{
    public override string Name => "AVERAGEIF";

    public override FunctionParameter[] Parameters =>
    [
        new("range", ParameterType.Collection, isRequired: true),
        new("criteria", ParameterType.Single, isRequired: true),
        new("average_range", ParameterType.Collection, isRequired: false)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var range = arguments.GetRange("range");
        var criteria = arguments.GetSingle("criteria");

        if (range is null || criteria is null)
        {
            return CellData.FromError(CellError.Value);
        }

        var averageRange = arguments.GetRange("average_range") ?? range;

        if (range.Count != averageRange.Count)
        {
            return CellData.FromError(CellError.Value);
        }

        var values = new List<double>();

        for (var i = 0; i < range.Count; i++)
        {
            if (!range[i].MatchesCriteria(criteria))
            {
                continue;
            }

            var cell = averageRange[i];

            if (cell.IsError)
            {
                return cell;
            }

            if (cell.Type == CellDataType.Number)
            {
                values.Add(cell.GetValueOrDefault<double>());
            }
        }

        return AggregationMethods.Average(values);
    }
}
