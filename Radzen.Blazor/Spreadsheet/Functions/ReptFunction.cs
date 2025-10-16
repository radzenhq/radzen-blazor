#nullable enable

using System.Text;

namespace Radzen.Blazor.Spreadsheet;

class ReptFunction : FormulaFunction
{
    public override string Name => "REPT";

    public override FunctionParameter[] Parameters =>
    [
        new("text", ParameterType.Single, isRequired: true),
        new("number_times", ParameterType.Single, isRequired: true)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        if (!TryGetString(arguments, "text", out var text, out var error))
        {
            return error!;
        }

        if (!TryGetInteger(arguments, "number_times", isRequired: true, defaultValue: null, out var times, out error))
        {
            return error!;
        }

        if (times < 0)
        {
            return CellData.FromError(CellError.Value);
        }

        if (times == 0 || text.Length == 0)
        {
            return CellData.FromString(string.Empty);
        }

        // Cap at 32,767
        var totalLength = (long)text.Length * times;

        if (totalLength > 32767)
        {
            return CellData.FromError(CellError.Value);
        }

        var sb = StringBuilderCache.Acquire((int)totalLength);
        for (int i = 0; i < times; i++)
        {
            sb.Append(text);
        }
        return CellData.FromString(StringBuilderCache.GetStringAndRelease(sb));
    }
}