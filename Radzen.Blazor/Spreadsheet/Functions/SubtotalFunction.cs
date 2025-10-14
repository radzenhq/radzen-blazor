#nullable enable

using System;
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
            1 => EvaluateAverage(numeric),
            2 => CellData.FromNumber(numeric.Count),
            3 => CellData.FromNumber(nonEmptyCount),
            4 => EvaluateMax(numeric),
            5 => EvaluateMin(numeric),
            9 => EvaluateSum(numeric),
            _ => CellData.FromError(CellError.Value)
        };
    }

    private static CellData EvaluateAverage(List<double> items)
    {
        if (items.Count == 0)
        {
            return CellData.FromError(CellError.Div0);
        }

        double sum = 0;

        foreach (var d in items)
        {
            sum += d;
        }

        return CellData.FromNumber(sum / items.Count);
    }

    private static CellData EvaluateSum(List<double> items)
    {
        double sum = 0;
        foreach (var d in items)
        {
            sum += d;
        }
        return CellData.FromNumber(sum);
    }

    private static CellData EvaluateMax(List<double> items)
    {
        if (items.Count == 0)
        {
            return CellData.FromNumber(0);
        }

        var max = items[0];

        for (int i = 1; i < items.Count; i++) 
        {
            if (items[i] > max)
            {
                max = items[i];
            }
        }
        return CellData.FromNumber(max);
    }

    private static CellData EvaluateMin(List<double> items)
    {
        if (items.Count == 0)
        {
            return CellData.FromNumber(0);
        }

        var min = items[0];

        for (int i = 1; i < items.Count; i++) 
        {
            if (items[i] < min)
            {
                min = items[i];
            }
        }

        return CellData.FromNumber(min);
    }
}