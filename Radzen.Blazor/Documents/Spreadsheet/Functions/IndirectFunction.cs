#nullable enable

using System.Globalization;
using System.Text.RegularExpressions;

namespace Radzen.Documents.Spreadsheet;

partial class IndirectFunction : ReferenceFunction
{
    public override string Name => "INDIRECT";

    internal override bool IsVolatile => true;

    public override FunctionParameter[] Parameters =>
    [
        new("ref_text", ParameterType.Single, isRequired: true),
        new("a1", ParameterType.Single, isRequired: false)
    ];

    public override string? GetReferenceText(FunctionArguments arguments, out CellData? error)
    {
        error = null;

        var text = arguments.GetSingle("ref_text")?.ToString();
        var a1 = arguments.GetSingle("a1");
        var isA1 = true;

        if (a1 is not null && !a1.IsEmpty)
        {
            if (a1.Type == CellDataType.Boolean)
            {
                isA1 = a1.GetValueOrDefault<bool>();
            }
            else if (a1.TryCoerceToNumber(out var number, allowBooleans: true, nonNumericTextAsZero: false))
            {
                isA1 = number != 0;
            }
            else
            {
                error = CellData.FromError(CellError.Value);
                return null;
            }
        }

        if (string.IsNullOrEmpty(text) || char.IsWhiteSpace(text[0]) || char.IsWhiteSpace(text[^1]))
        {
            return null;
        }

        return isA1 ? text : ConvertR1C1(text, arguments.CurrentCell.Address);
    }

    private static string? ConvertR1C1(string text, CellRef origin)
    {
        var match = R1C1Pattern().Match(text);

        if (!match.Success)
        {
            return null;
        }

        var start = ToA1(match.Groups["row"].Value, match.Groups["column"].Value, origin);

        if (start is null)
        {
            return null;
        }

        var reference = match.Groups["sheet"].Success ? $"{match.Groups["sheet"].Value}!{start}" : start;

        if (!match.Groups["end"].Success)
        {
            return reference;
        }

        var end = ToA1(match.Groups["endRow"].Value, match.Groups["endColumn"].Value, origin);

        return end is null ? null : $"{reference}:{end}";
    }

    private static string? ToA1(string row, string column, CellRef origin)
    {
        if (!TryResolve(row, origin.Row, out var rowIndex) || !TryResolve(column, origin.Column, out var columnIndex)
            || rowIndex < 0 || columnIndex < 0 || rowIndex >= Worksheet.MaxRows || columnIndex >= Worksheet.MaxColumns)
        {
            return null;
        }

        return new CellRef(rowIndex, columnIndex).ToString();
    }

    private static bool TryResolve(string part, int originIndex, out int index)
    {
        if (part.Length == 0)
        {
            index = originIndex;
            return true;
        }

        if (part[0] == '[')
        {
            var parsed = int.TryParse(part[1..^1], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var offset);
            index = originIndex + offset;
            return parsed;
        }

        var absolute = int.TryParse(part, NumberStyles.None, CultureInfo.InvariantCulture, out var number);
        index = number - 1;
        return absolute;
    }

    [GeneratedRegex(@"^(?:(?<sheet>'(?:[^']|'')+'|[^'!]+)!)?R(?<row>\[[+-]?\d+\]|\d+)?C(?<column>\[[+-]?\d+\]|\d+)?(?<end>:R(?<endRow>\[[+-]?\d+\]|\d+)?C(?<endColumn>\[[+-]?\d+\]|\d+)?)?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex R1C1Pattern();
}
