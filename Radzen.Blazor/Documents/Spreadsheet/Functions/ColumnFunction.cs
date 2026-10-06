#nullable enable

namespace Radzen.Documents.Spreadsheet;

class ColumnFunction : FormulaFunction
{
    public override string Name => "COLUMN";

    public override FunctionParameter[] Parameters =>
    [
        new("reference", ParameterType.Collection, isRequired: false) { IsReference = true }
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        if (arguments.GetReference("reference") is { } area)
        {
            if (area.Rows > 1 && area.Columns > 1)
            {
                return CellData.FromError(CellError.Value);
            }

            return CellData.FromNumber(area.StartColumn + 1);
        }

        var reference = arguments.GetRange("reference");

        if (reference is null)
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