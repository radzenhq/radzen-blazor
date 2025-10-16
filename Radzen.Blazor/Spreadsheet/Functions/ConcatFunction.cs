#nullable enable

using System.Text;

namespace Radzen.Blazor.Spreadsheet;

class ConcatFunction : FormulaFunction
{
    public override string Name => "CONCAT";

    public override FunctionParameter[] Parameters =>
    [
        new("text", ParameterType.Sequence, isRequired: true)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var values = arguments.GetSequence("text");

        if (values == null || values.Count == 0)
        {
            return CellData.FromError(CellError.Value);
        }

        var sb = StringBuilderCache.Acquire(values.Count * 2);

        foreach (var v in values)
        {
            if (v.IsError)
            {
                // Propagate first error encountered
                StringBuilderCache.Release(sb);
                return v;
            }

            var s = v.GetValueOrDefault<string?>() ?? string.Empty;
            sb.Append(s);
        }

        return CellData.FromString(StringBuilderCache.GetStringAndRelease(sb));
    }
}