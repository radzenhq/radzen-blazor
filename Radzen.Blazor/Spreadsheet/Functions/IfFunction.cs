using System.Collections.Generic;

#nullable enable

namespace Radzen.Blazor.Spreadsheet;

class IfFunction : FormulaFunction
{
    public override CellData Evaluate(List<CellData> arguments)
    {
        if (arguments.Count < 2 || arguments.Count > 3)
        {
            return CellData.FromError(CellError.Value);
        }

        var condition = arguments[0];

        if (condition.IsError)
        {
            return condition;
        }

        var trueValue = arguments[1];

        var falseValue = arguments.Count == 3 ? arguments[2] : CellData.FromBoolean(false);

        if (condition.IsEmpty)
        {
            return falseValue;
        }

        var value = condition.GetValueOrDefault<bool?>();

        if (value is null)
        {
            return CellData.FromError(CellError.Value);
        }
        else
        {
            return value.Value ? trueValue : falseValue;
        }
    }
}