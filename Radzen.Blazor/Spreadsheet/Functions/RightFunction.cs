#nullable enable

namespace Radzen.Blazor.Spreadsheet;

class RightFunction : TextExtractFunctionBase
{
    public override string Name => "RIGHT";

    public override FunctionParameter[] Parameters =>
    [
        new("text", ParameterType.Single, isRequired: true),
        new("num_chars", ParameterType.Single, isRequired: false)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        if (!TryGetString(arguments, "text", out var text, out var error))
        {
            return error!;
        }

        if (!TryGetInteger(arguments, "num_chars", isRequired: false, defaultValue: 1, out var count, out error))
        {
            return error!;
        }

        if (count < 0)
        {
            return CellData.FromError(CellError.Value);
        }

        var start = text.Length - count;
        if (start < 0) start = 0;

        return Substring(text, start, count);
    }
}