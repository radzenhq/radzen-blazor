#nullable enable

using System.Collections.Generic;

namespace Radzen.Blazor.Spreadsheet;

class SubtotalFunction : FormulaFunction
{
    public override string Name => "SUBTOTAL";

    public override FunctionParameter[] Parameters =>
    [
        new("function_num", ParameterType.Single, isRequired: true),
        new("ref", ParameterType.Collection, isRequired: true)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var funcArg = arguments.GetSingle("function_num");
        var values = arguments.GetRange("ref");

        if (funcArg == null || values == null)
        {
            return CellData.FromError(CellError.Value);
        }

        if (funcArg.IsError)
        {
            return funcArg;
        }

        var funcCode = funcArg.GetValueOrDefault<int?>();
        if (funcCode is null)
        {
            return CellData.FromError(CellError.Value);
        }

        // Normalize 101-111 to 1-11 (hidden rows handling not supported in this context)
        var code = funcCode.Value > 100 ? funcCode.Value - 100 : funcCode.Value;
        var excludeHidden = funcCode.Value >= 100;

        // Flatten numeric values; also collect non-empty for COUNTA
        var numeric = new List<double>();
        var nonEmptyCount = 0;

        // If we have coordinates and row-hidden info, use it
        RangeList? rangeList = values as RangeList;

        for (int i = 0; i < values.Count; i++)
        {
            var cell = values[i];

            if (cell.IsError)
            {
                return cell;
            }

            if (!cell.IsEmpty)
            {
                nonEmptyCount++;
            }

            // Hidden rows handling if available
            if (excludeHidden && rangeList != null)
            {
                if (rangeList.IsRowHiddenAt(i))
                {
                    continue;
                }
            }

            if (cell.Type == CellDataType.Number)
            {
                numeric.Add(cell.GetValueOrDefault<double>());
            }
        }

        return code switch
        {
            1 => AggregationMethods.Average(numeric),
            2 => CellData.FromNumber(numeric.Count),
            3 => CellData.FromNumber(nonEmptyCount),
            4 => AggregationMethods.Max(numeric),
            5 => AggregationMethods.Min(numeric),
            9 => AggregationMethods.Sum(numeric),
            _ => CellData.FromError(CellError.Value)
        };
    }
}