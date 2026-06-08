#nullable enable

namespace Radzen.Documents.Spreadsheet;

class CountIfFunction : FormulaFunction
{
    public override string Name => "COUNTIF";

    public override FunctionParameter[] Parameters =>
    [
        new("range", ParameterType.Collection, isRequired: true),
        new("criteria", ParameterType.Single, isRequired: true)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var range = arguments.GetRange("range");
        var criteria = arguments.GetSingle("criteria");

        if (range is null || criteria is null)
        {
            return CellData.FromError(CellError.Value);
        }

        var count = 0;

        foreach (var cell in range)
        {
            if (cell.MatchesCriteria(criteria))
            {
                count++;
            }
        }

        return CellData.FromNumber(count);
    }
}
