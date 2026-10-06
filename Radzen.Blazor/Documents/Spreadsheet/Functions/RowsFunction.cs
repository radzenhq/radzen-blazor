#nullable enable

namespace Radzen.Documents.Spreadsheet;

class RowsFunction : FormulaFunction
{
    public override string Name => "ROWS";

    public override FunctionParameter[] Parameters =>
    [
        new("array", ParameterType.Collection, isRequired: true) { IsReference = true }
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        if (arguments.GetReference("array") is { } reference)
        {
            return CellData.FromNumber(reference.LogicalRows);
        }

        var array = arguments.GetRange("array");

        if (array is RangeList range)
        {
            return CellData.FromNumber(range.LogicalRows);
        }

        return CellData.FromError(CellError.Value);
    }
}