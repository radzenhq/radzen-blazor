using System.Collections.Generic;

#nullable enable

namespace Radzen.Blazor.Spreadsheet;

class OrFunction : FormulaFunction
{
    public override CellData Evaluate(List<CellData> arguments)
    {
        if (arguments.Count == 0)
        {
            return CellData.FromError(CellError.Value);
        }

        bool? result = null;

        foreach (var argument in arguments)
        {
            if (argument.IsError)
            {
                return argument;
            }

            if (argument.IsEmpty)
            {
                continue;
            }

            var value = argument.GetValueOrDefault<bool?>();

            if (value is null && result is null)
            {
                continue;
            }

            result ??= false;

            result |= value;
        }

        if (result is null)
        {
            return CellData.FromError(CellError.Value);
        }

        return CellData.FromBoolean(result.Value);
    }
}
