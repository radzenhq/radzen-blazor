#nullable enable

using System.Text;

namespace Radzen.Blazor.Spreadsheet;

class TrimFunction : FormulaFunction
{
    public override string Name => "TRIM";

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

        var s = textArg.GetValueOrDefault<string?>() ?? string.Empty;

        var trimmed = s.Trim(' ');

        if (trimmed.Length == 0)
        {
            return CellData.FromString(string.Empty);
        }
        
        var sb = StringBuilderCache.Acquire(trimmed.Length);

        var lastWasSpace = false;

        foreach (var ch in trimmed)
        {
            if (ch == ' ')
            {
                if (!lastWasSpace)
                {
                    sb.Append(' ');
                    lastWasSpace = true;
                }
            }
            else
            {
                sb.Append(ch);
                lastWasSpace = false;
            }
        }

        return CellData.FromString(StringBuilderCache.GetStringAndRelease(sb));
    }
}