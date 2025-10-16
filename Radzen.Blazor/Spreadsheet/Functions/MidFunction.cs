#nullable enable

namespace Radzen.Blazor.Spreadsheet;

class MidFunction : TextExtractFunctionBase
{
    public override string Name => "MID";

    public override FunctionParameter[] Parameters =>
    [
        new("text", ParameterType.Single, isRequired: true),
        new("start_num", ParameterType.Single, isRequired: true),
        new("num_chars", ParameterType.Single, isRequired: true)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        if (!TryGetString(arguments, "text", out var text, out var error))
        {
            return error!;
        }

        if (!TryGetInteger(arguments, "start_num", isRequired: true, defaultValue: null, out var start, out error))
        {
            return error!;
        }

        if (!TryGetInteger(arguments, "num_chars", isRequired: true, defaultValue: null, out var numChars, out error))
        {
            return error!;
        }

        if (numChars < 0)
        {
            return CellData.FromError(CellError.Value);
        }

        if (start < 1)
        {
            return CellData.FromError(CellError.Value);
        }

        var zeroBasedStart = start - 1;
        return Substring(text, zeroBasedStart, numChars);
    }
}