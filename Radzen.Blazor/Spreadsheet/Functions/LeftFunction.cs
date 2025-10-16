#nullable enable

namespace Radzen.Blazor.Spreadsheet;

class LeftFunction : FormulaFunction
{
    public override string Name => "LEFT";

    public override FunctionParameter[] Parameters =>
    [
        new("text", ParameterType.Single, isRequired: true),
        new("num_chars", ParameterType.Single, isRequired: false)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var textArg = arguments.GetSingle("text");
        var numCharsArg = arguments.GetSingle("num_chars");

        if (textArg == null)
        {
            return CellData.FromError(CellError.Value);
        }

        if (textArg.IsError)
        {
            return textArg;
        }

        if (numCharsArg != null && numCharsArg.IsError)
        {
            return numCharsArg;
        }

        var text = textArg.GetValueOrDefault<string?>() ?? string.Empty;

        var count = 1;

        if (numCharsArg != null)
        {
            if (!numCharsArg.TryGetInt(out count, allowBooleans: true, nonNumericTextAsZero: false))
            {
                return CellData.FromError(CellError.Value);
            }
        }

        if (count < 0)
        {
            return CellData.FromError(CellError.Value);
        }

        if (count >= text.Length)
        {
            return CellData.FromString(text);
        }

        return CellData.FromString(text.Substring(0, count));
    }
}