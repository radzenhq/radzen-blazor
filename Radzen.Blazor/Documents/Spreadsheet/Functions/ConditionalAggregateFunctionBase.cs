#nullable enable

using System.Collections.Generic;

namespace Radzen.Documents.Spreadsheet;

// Base for multi-criteria aggregations with a leading value range (SUMIFS, AVERAGEIFS, MAXIFS, MINIFS):
// FUNC(value_range, criteria_range1, criteria1, ...).
abstract class ConditionalAggregateFunctionBase : FormulaFunction
{
    protected abstract CellData Aggregate(List<double> values);

    public override FunctionParameter[] Parameters =>
    [
        new("value_range", ParameterType.Collection, isRequired: true),
        new("criteria", ParameterType.Group, isRequired: true)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var valueRange = arguments.GetRange("value_range");
        var groups = arguments.GetGroups("criteria");

        if (valueRange is null)
        {
            return CellData.FromError(CellError.Value);
        }

        if (!CriteriaPairs.TryMatch(groups, out var matched, out var error))
        {
            return error!;
        }

        if (groups![0].Count != valueRange.Count)
        {
            return CellData.FromError(CellError.Value);
        }

        var values = new List<double>();

        foreach (var i in matched)
        {
            var cell = valueRange[i];

            if (cell.IsError)
            {
                return cell;
            }

            if (cell.Type == CellDataType.Number)
            {
                values.Add(cell.GetValueOrDefault<double>());
            }
        }

        return Aggregate(values);
    }
}
