#nullable enable

namespace Radzen.Blazor.Spreadsheet;

class RowsFunction : FormulaFunction
{
    public override string Name => "ROWS";

    public override FunctionParameter[] Parameters =>
    [
        new("array", ParameterType.Collection, isRequired: true)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var array = arguments.GetRange("array");

        if (array is RangeList range)
        {
            return CellData.FromNumber(range.Rows);
        }

        return CellData.FromError(CellError.Value);
    }
}