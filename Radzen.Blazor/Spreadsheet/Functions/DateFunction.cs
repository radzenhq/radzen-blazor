#nullable enable

using System;

namespace Radzen.Blazor.Spreadsheet;

class DateFunction : FormulaFunction
{
    public override string Name => "DATE";

    public override FunctionParameter[] Parameters =>
    [
        new("year", ParameterType.Single, isRequired: true),
        new("month", ParameterType.Single, isRequired: true),
        new("day", ParameterType.Single, isRequired: true)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        if (!TryGetInteger(arguments, "year", true, null, out var year, out var error))
        {
            return error!;
        }

        if (!TryGetInteger(arguments, "month", true, null, out var month, out var error2))
        {
            return error2!;
        }

        if (!TryGetInteger(arguments, "day", true, null, out var day, out var error3))
        {
            return error3!;
        }

        // Excel: years 0-1899 are offset from 1900
        if (year >= 0 && year <= 1899)
        {
            year += 1900;
        }

        try
        {
            // Start from Jan 1 of the given year, then offset month and day
            // This handles overflow/underflow (e.g. month 13 = Jan next year, day 0 = last day of prev month)
            var date = new DateTime(year, 1, 1)
                .AddMonths(month - 1)
                .AddDays(day - 1);

            return CellData.FromDate(date);
        }
        catch (ArgumentOutOfRangeException)
        {
            return CellData.FromError(CellError.Num);
        }
    }
}
