#nullable enable

namespace Radzen.Blazor.Spreadsheet;

class LenFunction : FormulaFunction
{
    public override string Name => "LEN";

    public override FunctionParameter[] Parameters =>
    [
        new("text", ParameterType.Single, isRequired: true)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var textArg = arguments.GetSingle("text");

        if (textArg == null)
        {
            return CellData.FromError(CellError.Value);
        }

        if (textArg.IsError)
        {
            return textArg;
        }

        var value = textArg.GetValueOrDefault<string?>();

        return CellData.FromNumber(value?.Length ?? 0);
    }
}