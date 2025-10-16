#nullable enable

using System.Text;
using System.Text.RegularExpressions;

namespace Radzen.Blazor.Spreadsheet;

class SearchFunction : FormulaFunction
{
    public override string Name => "SEARCH";

    public override FunctionParameter[] Parameters =>
    [
        new("find_text", ParameterType.Single, isRequired: true),
        new("within_text", ParameterType.Single, isRequired: true),
        new("start_num", ParameterType.Single, isRequired: false)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        if (!TryGetString(arguments, "find_text", out var findText, out var error))
        {
            return error!;
        }

        if (!TryGetString(arguments, "within_text", out var withinText, out error))
        {
            return error!;
        }

        if (!TryGetInteger(arguments, "start_num", isRequired: false, defaultValue: 1, out var startNum, out error))
        {
            return error!;
        }

        if (startNum <= 0 || startNum > withinText.Length)
        {
            return CellData.FromError(CellError.Value);
        }

        var startIndex = startNum - 1; // zero-based

        // Empty findText returns start_num per Excel behavior
        if (findText.Length == 0)
        {
            return CellData.FromNumber(startNum);
        }

        int pos;

        if (findText.Contains('*') || findText.Contains('?') || findText.Contains('~'))
        {
            var idx = FindWithWildcards(findText, withinText, startIndex);
            pos = idx;
        }
        else
        {
            pos = withinText.IndexOf(findText, startIndex, System.StringComparison.OrdinalIgnoreCase);
        }

        if (pos < 0)
        {
            return CellData.FromError(CellError.Value);
        }

        return CellData.FromNumber(pos + 1);
    }

    private static int FindWithWildcards(string pattern, string text, int startIndex)
    {
        var regexBuilder = StringBuilderCache.Acquire(pattern.Length);
        for (int i = 0; i < pattern.Length; i++)
        {
            var ch = pattern[i];
            if (ch == '~')
            {
                if (i + 1 < pattern.Length)
                {
                    var next = pattern[++i];
                    // Escape the next character literally
                    regexBuilder.Append(Regex.Escape(next.ToString()));
                }
                else
                {
                    // Trailing ~ treated as literal
                    regexBuilder.Append(Regex.Escape("~"));
                }
            }
            else if (ch == '*')
            {
                regexBuilder.Append(".*");
            }
            else if (ch == '?')
            {
                regexBuilder.Append('.');
            }
            else
            {
                regexBuilder.Append(Regex.Escape(ch.ToString()));
            }
        }

        var regex = new Regex(StringBuilderCache.GetStringAndRelease(regexBuilder), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        var input = startIndex > 0 ? text[startIndex..] : text;
        var match = regex.Match(input);
        if (!match.Success)
        {
            return -1;
        }
        return startIndex + match.Index;
    }
}