#nullable enable

namespace Radzen.Blazor.Spreadsheet;

class ReplaceFunction : FormulaFunction
{
    public override string Name => "REPLACE";

    public override FunctionParameter[] Parameters =>
    [
        new("old_text", ParameterType.Single, isRequired: true),
        new("start_num", ParameterType.Single, isRequired: true),
        new("num_chars", ParameterType.Single, isRequired: true),
        new("new_text", ParameterType.Single, isRequired: true)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        if (!TryGetString(arguments, "old_text", out var oldText, out var error))
        {
            return error!;
        }

        if (!TryGetInteger(arguments, "start_num", isRequired: true, defaultValue: null, out var startNum, out error))
        {
            return error!;
        }

        if (!TryGetInteger(arguments, "num_chars", isRequired: true, defaultValue: null, out var numChars, out error))
        {
            return error!;
        }

        if (!TryGetString(arguments, "new_text", out var newText, out error))
        {
            return error!;
        }

        if (startNum < 1 || numChars < 0)
        {
            return CellData.FromError(CellError.Value);
        }

        var zeroStart = startNum - 1;
        if (zeroStart > oldText.Length)
        {
            return CellData.FromString(oldText + newText);
        }

        var remove = numChars;
        if (zeroStart + remove > oldText.Length)
        {
            remove = oldText.Length - zeroStart;
        }

        var prefix = oldText.Substring(0, zeroStart);
        var suffix = oldText.Substring(zeroStart + remove);
        return CellData.FromString(prefix + newText + suffix);
    }
}