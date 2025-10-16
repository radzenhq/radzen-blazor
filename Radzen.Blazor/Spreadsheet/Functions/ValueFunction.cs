#nullable enable

using System;
using System.Globalization;
using System.Text;

namespace Radzen.Blazor.Spreadsheet;

class ValueFunction : FormulaFunction
{
    public override string Name => "VALUE";

    public override FunctionParameter[] Parameters =>
    [
        new("text", ParameterType.Single, isRequired: true)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var arg = arguments.GetSingle("text");
        if (arg == null)
        {
            return CellData.FromError(CellError.Value);
        }

        if (arg.IsError)
        {
            return arg;
        }

        // If already number, return as-is
        if (arg.Type == CellDataType.Number)
        {
            return arg;
        }

        // If date, convert to serial
        if (arg.Type == CellDataType.Date)
        {
            var dt = arg.GetValueOrDefault<DateTime>();
            return CellData.FromNumber(dt.ToNumber());
        }

        // If boolean, Excel VALUE("TRUE") is #VALUE!; only text date/time/number formats allowed
        var s = arg.GetValueOrDefault<string?>() ?? string.Empty;

        // Try numeric (allow currency/grouping). Invariant parsing does not accept '$', so sanitize.
        if (double.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var num))
        {
            return CellData.FromNumber(num);
        }

        // Try time
        if (TimeSpan.TryParse(s, CultureInfo.InvariantCulture, out var ts))
        {
            // Excel stores times as fraction of a day. Round to 15 decimals to stabilize binary representation.
            var fraction = Math.Round(ts.TotalDays, 15, MidpointRounding.AwayFromZero);
            return CellData.FromNumber(fraction);
        }

        // Try date/datetime
        if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt2))
        {
            return CellData.FromNumber(dt2.ToNumber());
        }

        var sb = StringBuilderCache.Acquire(s.Length);

        foreach (var c in s)
        {
            if (char.IsNumber(c) || c == '.' || c == '-' || c == '+' || c == '(' || c == ')')
            {
                sb.Append(c);
            }
        }

        var sanitized = StringBuilderCache.GetStringAndRelease(sb);

        var isNegativeParen = sanitized.Length >= 2 && sanitized[0] == '(' && sanitized[^1] == ')';

        if (isNegativeParen)
        {
            sanitized = sanitized[1..^1];
        }

        if (double.TryParse(sanitized, NumberStyles.Number | NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out num))
        {
            return CellData.FromNumber(isNegativeParen ? -num : num);
        }


        return CellData.FromError(CellError.Value);
    }
}