#nullable enable

using System.Text;

namespace Radzen.Blazor.Spreadsheet;

class TextJoinFunction : FormulaFunction
{
    public override string Name => "TEXTJOIN";

    public override FunctionParameter[] Parameters =>
    [
        new("delimiter", ParameterType.Single, isRequired: true),
        new("ignore_empty", ParameterType.Single, isRequired: true),
        new("text", ParameterType.Sequence, isRequired: true)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var delimiterArg = arguments.GetSingle("delimiter");
        var ignoreArg = arguments.GetSingle("ignore_empty");
        var texts = arguments.GetSequence("text");

        if (delimiterArg == null || ignoreArg == null || texts == null)
        {
            return CellData.FromError(CellError.Value);
        }

        if (delimiterArg.IsError)
        {
            return delimiterArg;
        }

        if (ignoreArg.IsError)
        {
            return ignoreArg;
        }

        // Parse ignore_empty as boolean (accept booleans or numeric/text coercible to boolean/number)
        bool ignoreEmpty;
        if (ignoreArg.Type == CellDataType.Boolean)
        {
            ignoreEmpty = ignoreArg.GetValueOrDefault<bool>();
        }
        else if (ignoreArg.TryCoerceToNumber(out var num, allowBooleans: true, nonNumericTextAsZero: false))
        {
            ignoreEmpty = num != 0d;
        }
        else
        {
            return CellData.FromError(CellError.Value);
        }

        // Single delimiter string
        var delimiter = delimiterArg.GetValueOrDefault<string?>() ?? string.Empty;

        var sb = StringBuilderCache.Acquire(texts.Count * 2);
        var wroteOne = false;

        foreach (var v in texts)
        {
            if (v.IsError)
            {
                StringBuilderCache.Release(sb);
                return v;
            }

            var s = v.GetValueOrDefault<string?>() ?? string.Empty;
            if (ignoreEmpty && s.Length == 0)
            {
                continue;
            }

            if (wroteOne)
            {
                sb.Append(delimiter);
            }

            sb.Append(s);
            wroteOne = true;
        }

        return CellData.FromString(StringBuilderCache.GetStringAndRelease(sb));
    }
}


