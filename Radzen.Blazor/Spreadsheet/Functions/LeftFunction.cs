#nullable enable

namespace Radzen.Blazor.Spreadsheet;

class LeftFunction : TextExtractFunctionBase
{
    public override string Name => "LEFT";

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

        return Substring(text, 0, count);
    }
}