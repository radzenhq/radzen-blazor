using System.Collections.Generic;

#nullable enable

namespace Radzen.Blazor.Spreadsheet;

class IfErrorFunction : FormulaFunction
{
    public override bool CanHandleErrors => true;

    public override CellData Evaluate(List<CellData> arguments)
    {
        if (arguments.Count != 2)
        {
            return new CellData(CellError.Value);
        }

        var value = arguments[0];
        var valueIfError = arguments[1];

        if (value.IsError)
        {
            return valueIfError.IsEmpty ? CellData.FromString("") : valueIfError;
        }

        return value.IsEmpty ? CellData.FromString("") : value;
    }
}
