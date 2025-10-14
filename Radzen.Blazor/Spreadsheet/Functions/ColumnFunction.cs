#nullable enable

namespace Radzen.Blazor.Spreadsheet;

class ColumnFunction : FormulaFunction
{
    public override string Name => "COLUMN";

    public override FunctionParameter[] Parameters =>
    [
        new("reference", ParameterType.Collection, isRequired: false)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var reference = arguments.GetRange("reference");

        if (reference == null)
        {
            return CellData.FromNumber(arguments.CurrentCell.Address.Column + 1);
        }

        if (reference is RangeList range)
        {
            if (range.Rows > 1 && range.Columns > 1)
            {
                return CellData.FromError(CellError.Value);
            }

            return CellData.FromNumber(range.StartColumn + 1);
        }

        return CellData.FromError(CellError.Value);
    }
}


