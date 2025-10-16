#nullable enable

namespace Radzen.Blazor.Spreadsheet;

class MidFunction : FormulaFunction
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
        var textArg = arguments.GetSingle("text");
        var startArg = arguments.GetSingle("start_num");
        var lenArg = arguments.GetSingle("num_chars");

        if (textArg == null || startArg == null || lenArg == null)
        {
            return CellData.FromError(CellError.Value);
        }

        if (textArg.IsError)
        {
            return textArg;
        }

        if (startArg.IsError)
        {
            return startArg;
        }

        if (lenArg.IsError)
        {
            return lenArg;
        }

        var text = textArg.GetValueOrDefault<string?>() ?? string.Empty;

        if (!startArg.TryGetInt(out var start, allowBooleans: true, nonNumericTextAsZero: false))
        {
            return CellData.FromError(CellError.Value);
        }

        if (!lenArg.TryGetInt(out var numChars, allowBooleans: true, nonNumericTextAsZero: false))
        {
            return CellData.FromError(CellError.Value);
        }

        if (numChars < 0)
        {
            return CellData.FromError(CellError.Value);
        }

        if (start < 1)
        {
            return CellData.FromError(CellError.Value);
        }

        if (start > text.Length)
        {
            return CellData.FromString(string.Empty);
        }

        var zeroBasedStart = start - 1;
        var available = text.Length - zeroBasedStart;
        var take = numChars > available ? available : numChars;

        return CellData.FromString(text.Substring(zeroBasedStart, take));
    }
}