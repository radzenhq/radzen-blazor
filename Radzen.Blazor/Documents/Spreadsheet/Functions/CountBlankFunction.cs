#nullable enable

namespace Radzen.Documents.Spreadsheet;

class CountBlankFunction : FormulaFunction
{
    public override string Name => "COUNTBLANK";

    public override FunctionParameter[] Parameters =>
    [
        new("range", ParameterType.Collection, isRequired: true)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var range = arguments.GetRange("range");

        if (range is null)
        {
            return CellData.FromError(CellError.Value);
        }

        var count = 0;

        foreach (var cell in range)
        {
            // Excel counts both truly empty cells and formula-produced empty strings.
            if (cell.IsEmpty || (cell.Type == CellDataType.String && string.IsNullOrEmpty(cell.GetValueOrDefault<string>())))
            {
                count++;
            }
        }

        return CellData.FromNumber(count);
    }
}
