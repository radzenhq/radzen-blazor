#nullable enable

namespace Radzen.Documents.Spreadsheet;

class SwitchFunction : FormulaFunction
{
    public override string Name => "SWITCH";

    public override bool CanHandleErrors => true;

    public override FunctionParameter[] Parameters =>
    [
        new("expression", ParameterType.Single, isRequired: true),
        new("args", ParameterType.Group, isRequired: true) { IsLazy = true }
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var expression = arguments.GetSingle("expression");
        var groups = arguments.GetLazyGroups("args");

        if (expression is null || groups is null || groups.Count == 0)
        {
            return CellData.FromError(CellError.Value);
        }

        if (expression.IsError)
        {
            return expression;
        }

        var pairCount = groups.Count / 2;

        for (var p = 0; p < pairCount; p++)
        {
            var value = groups[p * 2].Value;

            if (value.IsError)
            {
                return value;
            }

            if (expression.IsEqualTo(value))
            {
                return groups[p * 2 + 1].Value;
            }
        }

        // An odd trailing argument is the default.
        if (groups.Count % 2 == 1)
        {
            return groups[^1].Value;
        }

        return CellData.FromError(CellError.NA);
    }
}
