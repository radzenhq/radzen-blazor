#nullable enable

namespace Radzen.Blazor.Spreadsheet;

class ColumnsFunction : FormulaFunction
{
    public override string Name => "COLUMNS";

    public override FunctionParameter[] Parameters =>
    [
        new("array", ParameterType.Collection, isRequired: true)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var array = arguments.GetRange("array");

        if (array is RangeList range)
        {
            return CellData.FromNumber(range.Columns);
        }

        return CellData.FromError(CellError.Value);
    }
}