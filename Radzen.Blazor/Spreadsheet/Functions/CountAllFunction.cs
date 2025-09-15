using System.Collections.Generic;

#nullable enable

namespace Radzen.Blazor.Spreadsheet;

class CountAllFunction : FormulaFunction
{
    public override CellData Evaluate(List<CellData> arguments)
    {
        if (arguments.Count == 0)
        {
            return CellData.FromNumber(0d);
        }

        var count = 0d;

        foreach (var v in arguments)
        {
            if (v.IsEmpty)
            {
                continue;
            }

            count++;
        }

        return CellData.FromNumber(count);
    }
}