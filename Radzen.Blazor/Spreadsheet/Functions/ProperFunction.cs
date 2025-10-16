#nullable enable

using System.Text;

namespace Radzen.Blazor.Spreadsheet;

class ProperFunction : FormulaFunction
{
    public override string Name => "PROPER";

    public override FunctionParameter[] Parameters =>
    [
        new("text", ParameterType.Single, isRequired: true)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        if (!TryGetString(arguments, "text", out var text, out var error))
        {
            return error!;
        }

        if (text.Length == 0)
        {
            return CellData.FromString(string.Empty);
        }

        // Capitalize first letter of words and letters following non-letters; others to lowercase
        var capitalizeNext = true;
        var sb = StringBuilderCache.Acquire(text.Length);
        foreach (var c in text)
        {
            if (char.IsLetter(c))
            {
                sb.Append(capitalizeNext ? char.ToUpperInvariant(c) : char.ToLowerInvariant(c));
                capitalizeNext = false;
            }
            else
            {
                sb.Append(c);
                capitalizeNext = true;
            }
        }

        return CellData.FromString(StringBuilderCache.GetStringAndRelease(sb));
    }
}