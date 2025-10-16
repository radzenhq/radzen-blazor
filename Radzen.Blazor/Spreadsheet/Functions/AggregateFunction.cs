#nullable enable

using System;
using System.Collections.Generic;

namespace Radzen.Blazor.Spreadsheet;

class AggregateFunction : FormulaFunction
{
    public override string Name => "AGGREGATE";

    public override FunctionParameter[] Parameters =>
    [
        new("function_num", ParameterType.Single, isRequired: true),
        new("options", ParameterType.Single, isRequired: true),
        new("array", ParameterType.Collection, isRequired: true),
        new("k", ParameterType.Single, isRequired: false)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var funcArg = arguments.GetSingle("function_num");
        var optsArg = arguments.GetSingle("options");
        var range = arguments.GetRange("array");
        var kArg = arguments.GetSingle("k");

        if (funcArg == null || optsArg == null || range == null)
        {
            return CellData.FromError(CellError.Value);
        }

        if (funcArg.IsError) return funcArg;
        if (optsArg.IsError) return optsArg;

        var func = funcArg.GetValueOrDefault<int?>();
        var opts = optsArg.GetValueOrDefault<int?>();
        if (func is null || opts is null)
        {
            return CellData.FromError(CellError.Value);
        }

        var ignoreHidden = opts.Value is 1 or 3 or 5 or 7;
        var ignoreErrors = opts.Value is 2 or 3 or 6 or 7;

        // Collect numeric values according to options
        var numbers = new List<double>();
        var nonEmptyCount = 0;

        RangeList? rl = range as RangeList;

        for (int i = 0; i < range.Count; i++)
        {
            var cell = range[i];

            // hidden rows
            if (ignoreHidden && rl != null && rl.IsRowHiddenAt(i))
            {
                continue;
            }

            // nested SUBTOTAL/AGGREGATE: not implemented in this version

            if (cell.IsError)
            {
                if (!ignoreErrors)
                {
                    return cell;
                }
                continue;
            }

            if (!cell.IsEmpty) nonEmptyCount++;

            if (cell.Type == CellDataType.Number)
            {
                numbers.Add(cell.GetValueOrDefault<double>());
            }
        }

        switch (func.Value)
        {
            case 1: // AVERAGE
                return AggregationMethods.Average(numbers);
            case 2: // COUNT
                return CellData.FromNumber(numbers.Count);
            case 3: // COUNTA
                return CellData.FromNumber(nonEmptyCount);
            case 4: // MAX
                return AggregationMethods.Max(numbers);
            case 5: // MIN
                return AggregationMethods.Min(numbers);
            case 9: // SUM
                return AggregationMethods.Sum(numbers);
            case 12: // MEDIAN
                return AggregationMethods.Median(numbers);
            case 14: // LARGE
            {
                if (kArg == null || kArg.IsError) 
                {
                    return kArg ?? CellData.FromError(CellError.Value);
                }
                var k = kArg.GetValueOrDefault<int?>();
                if (k is null) 
                {
                    return CellData.FromError(CellError.Value);
                }
                return AggregationMethods.Large(numbers, k.Value);
            }
            case 15: // SMALL
            {
                if (kArg == null || kArg.IsError) 
                {
                    return kArg ?? CellData.FromError(CellError.Value);
                }
                var k = kArg.GetValueOrDefault<int?>();
                if (k is null) 
                {
                    return CellData.FromError(CellError.Value);
                }
                return AggregationMethods.Small(numbers, k.Value);
            }
            default:
                return CellData.FromError(CellError.Value);
        }
    }
}