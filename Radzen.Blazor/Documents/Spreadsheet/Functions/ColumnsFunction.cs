#nullable enable

namespace Radzen.Documents.Spreadsheet;

class ColumnsFunction : FormulaFunction
{
    public override string Name => "COLUMNS";

    public override FunctionParameter[] Parameters =>
    [
        new("array", ParameterType.Collection, isRequired: true) { IsReference = true }
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        if (arguments.GetReference("array") is { } reference)
        {
            return CellData.FromNumber(reference.LogicalColumns);
        }

        var array = arguments.GetRange("array");

        if (array is RangeList range)
        {
            return CellData.FromNumber(range.LogicalColumns);
        }

        return CellData.FromError(CellError.Value);
    }
}