using System;
using System.Collections.Generic;

#nullable enable

namespace Radzen.Blazor.Spreadsheet;

class AverageFunction : FormulaFunction
{
    public override CellData Evaluate(List<CellData> arguments)
    {
        if (arguments.Count == 0)
        {
            return CellData.FromError(CellError.Div0);
        }

        var sum = 0d;
        var count = 0;

        foreach (var argument in arguments)
        {
            if (argument.IsError)
            {
                return argument;
            }

            if (argument.IsEmpty || argument.Type != CellDataType.Number)
            {
                continue;
            }

            var value = argument.GetValueOrDefault<double>();

            sum += value;
            count++;
        }

        if (count == 0)
        {
            return CellData.FromError(CellError.Div0);
        }

        return CellData.FromNumber(sum / count);
    }
}