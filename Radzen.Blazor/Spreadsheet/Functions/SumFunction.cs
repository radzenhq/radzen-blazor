using System.Collections.Generic;

#nullable enable

namespace Radzen.Blazor.Spreadsheet;

class SumFunction : FormulaFunction
{
    public override CellData Evaluate(List<CellData> arguments)
    {
        if (arguments.Count == 0)
        {
            return CellData.FromError(CellError.Value);
        }

        var sum = 0d;

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
        }

        return CellData.FromNumber(sum);
    }
}