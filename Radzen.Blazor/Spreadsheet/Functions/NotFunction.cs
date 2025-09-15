using System.Collections.Generic;

#nullable enable

namespace Radzen.Blazor.Spreadsheet;

class NotFunction : FormulaFunction
{
    public override CellData Evaluate(List<CellData> arguments)
    {
        if (arguments.Count != 1)
        {
            return CellData.FromError(CellError.Value);
        }

        var argument = arguments[0];

        if (argument.IsError)
        {
            return argument;
        }

        if (argument.IsEmpty)
        {
            return CellData.FromBoolean(true);
        }

        var value = argument.GetValueOrDefault<bool?>();

        if (value is null)
        {
            return CellData.FromError(CellError.Value);
        }

        return CellData.FromBoolean(!value.Value);
    }
}