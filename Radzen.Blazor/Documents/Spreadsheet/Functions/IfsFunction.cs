#nullable enable

namespace Radzen.Documents.Spreadsheet;

class IfsFunction : FormulaFunction
{
    public override string Name => "IFS";

    public override bool CanHandleErrors => true;

    public override FunctionParameter[] Parameters =>
    [
        new("args", ParameterType.Group, isRequired: true)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var groups = arguments.GetGroups("args");

        if (groups is null || groups.Count == 0 || groups.Count % 2 != 0)
        {
            return CellData.FromError(CellError.Value);
        }

        for (var i = 0; i < groups.Count; i += 2)
        {
            var condition = groups[i].Count > 0 ? groups[i][0] : CellData.FromError(CellError.Value);

            if (condition.IsError)
            {
                return condition;
            }

            if (condition.IsEmpty)
            {
                continue;
            }

            var value = condition.GetValueOrDefault<bool?>();

            if (value is null)
            {
                return CellData.FromError(CellError.Value);
            }

            if (value.Value)
            {
                return groups[i + 1].Count > 0 ? groups[i + 1][0] : CellData.FromError(CellError.Value);
            }
        }

        return CellData.FromError(CellError.NA);
    }
}
