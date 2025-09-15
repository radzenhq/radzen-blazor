using System;
using System.Collections.Generic;

#nullable enable

namespace Radzen.Blazor.Spreadsheet;

class CountFunction : FormulaFunction
{
    public override CellData Evaluate(List<CellData> arguments)
    {
        if (arguments.Count == 0)
        {
            return CellData.FromNumber(0);
        }

        var count = 0d;

        foreach (var argument in arguments)
        {
            if (argument.IsError || argument.IsEmpty)
            {
                continue;
            }

            var value = argument.GetValueOrDefault<double?>();

            if (value is not null)
            {
                count++;
            }
        }

        return CellData.FromNumber(count);
    }
}