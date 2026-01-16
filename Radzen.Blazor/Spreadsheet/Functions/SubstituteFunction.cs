#nullable enable

namespace Radzen.Blazor.Spreadsheet;

class SubstituteFunction : FormulaFunction
{
    public override string Name => "SUBSTITUTE";

    public override FunctionParameter[] Parameters =>
    [
        new("text", ParameterType.Single, isRequired: true),
        new("old_text", ParameterType.Single, isRequired: true),
        new("new_text", ParameterType.Single, isRequired: true),
        new("instance_num", ParameterType.Single, isRequired: false)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        if (!TryGetString(arguments, "text", out var text, out var error))
        {
            return error!;
        }

        if (!TryGetString(arguments, "old_text", out var oldText, out error))
        {
            return error!;
        }

        if (!TryGetString(arguments, "new_text", out var newText, out error))
        {
            return error!;
        }

        if (!TryGetInteger(arguments, "instance_num", isRequired: false, defaultValue: 0, out var instanceNum, out error))
        {
            return error!;
        }

        if (oldText.Length == 0)
        {
            return CellData.FromError(CellError.Value);
        }

        if (instanceNum < 0)
        {
            return CellData.FromError(CellError.Value);
        }

        if (instanceNum == 0)
        {
            if (oldText == newText)
            {
                return CellData.FromString(text);
            }
            return CellData.FromString(text.Replace(oldText, newText, System.StringComparison.Ordinal));
        }
        else
        {
            // Replace nth occurrence only
            var index = -1;
            var count = 0;
            var start = 0;
            while (start <= text.Length - oldText.Length)
            {
                index = text.IndexOf(oldText, start, System.StringComparison.Ordinal);
                if (index < 0)
                {
                    break;
                }
                count++;
                if (count == instanceNum)
                {
                    // Build result: prefix + newText + suffix after oldText
                    var prefix = text.Substring(0, index);
                    var suffix = text.Substring(index + oldText.Length);
                    return CellData.FromString(prefix + newText + suffix);
                }
                start = index + oldText.Length;
            }

            // If the nth occurrence isn't found, return original text
            return CellData.FromString(text);
        }
    }
}