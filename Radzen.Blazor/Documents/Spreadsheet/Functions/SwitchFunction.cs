#nullable enable

namespace Radzen.Documents.Spreadsheet;

class SwitchFunction : FormulaFunction
{
    public override string Name => "SWITCH";

    public override bool CanHandleErrors => true;

    public override FunctionParameter[] Parameters =>
    [
        new("expression", ParameterType.Single, isRequired: true),
        new("args", ParameterType.Group, isRequired: true)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var expression = arguments.GetSingle("expression");
        var groups = arguments.GetGroups("args");

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
            var value = groups[p * 2].Count > 0 ? groups[p * 2][0] : CellData.FromError(CellError.Value);

            if (value.IsError)
            {
                return value;
            }

            if (expression.IsEqualTo(value))
            {
                var result = groups[p * 2 + 1];
                return result.Count > 0 ? result[0] : CellData.FromError(CellError.Value);
            }
        }

        // An odd trailing argument is the default.
        if (groups.Count % 2 == 1)
        {
            var defaultResult = groups[^1];
            return defaultResult.Count > 0 ? defaultResult[0] : CellData.FromError(CellError.Value);
        }

        return CellData.FromError(CellError.NA);
    }
}
