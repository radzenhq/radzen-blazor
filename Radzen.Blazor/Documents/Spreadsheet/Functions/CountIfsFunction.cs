#nullable enable

namespace Radzen.Documents.Spreadsheet;

class CountIfsFunction : FormulaFunction
{
    public override string Name => "COUNTIFS";

    public override FunctionParameter[] Parameters =>
    [
        new("criteria", ParameterType.Group, isRequired: true)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var groups = arguments.GetGroups("criteria");

        if (!CriteriaPairs.TryMatch(groups, out var matched, out var error))
        {
            return error!;
        }

        return CellData.FromNumber(matched.Count);
    }
}
