#nullable enable

using System;
using System.Globalization;

namespace Radzen.Blazor.Spreadsheet;

class WeeknumFunction : FormulaFunction
{
    public override string Name => "WEEKNUM";

    public override FunctionParameter[] Parameters =>
    [
        new("serial_number", ParameterType.Single, isRequired: true),
        new("return_type", ParameterType.Single, isRequired: false)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var dateArg = arguments.GetSingle("serial_number");
        if (dateArg == null)
        {
            return CellData.FromError(CellError.Value);
        }
        if (dateArg.IsError)
        {
            return dateArg;
        }

        if (!dateArg.TryCoerceToDate(out var date))
        {
            return CellData.FromError(CellError.Value);
        }

        // Determine return_type (default 1)
        var returnType = 1;
        var returnTypeArg = arguments.GetSingle("return_type");
        if (returnTypeArg != null)
        {
            if (returnTypeArg.IsError)
            {
                return returnTypeArg;
            }
            if (!returnTypeArg.TryGetInt(out returnType, allowBooleans: true, nonNumericTextAsZero: false))
            {
                return CellData.FromError(CellError.Value);
            }
        }

        // Validate return type
        if (!(returnType == 1 || returnType == 2 || (returnType >= 11 && returnType <= 17) || returnType == 21))
        {
            return CellData.FromError(CellError.Num);
        }

        if (returnType == 21)
        {
            // ISO 8601 week number: Monday as first day, FirstFourDayWeek
            var week = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
                date,
                CalendarWeekRule.FirstFourDayWeek,
                DayOfWeek.Monday);
            return CellData.FromNumber(week);
        }

        // System 1: week containing Jan 1 is week 1, with configurable week start
        var weekStart = GetWeekStartDay(returnType);
        var jan1 = new DateTime(date.Year, 1, 1);

        var daysSinceJan1 = (int)(date.Date - jan1.Date).TotalDays;
        var jan1Adjustment = ((int)jan1.DayOfWeek - (int)weekStart + 7) % 7;
        var weekNum = (daysSinceJan1 + jan1Adjustment) / 7 + 1;

        return CellData.FromNumber(weekNum);
    }

    private static DayOfWeek GetWeekStartDay(int returnType)
    {
        return returnType switch
        {
            1 or 17 => DayOfWeek.Sunday,
            2 or 11 => DayOfWeek.Monday,
            12 => DayOfWeek.Tuesday,
            13 => DayOfWeek.Wednesday,
            14 => DayOfWeek.Thursday,
            15 => DayOfWeek.Friday,
            16 => DayOfWeek.Saturday,
            _ => DayOfWeek.Sunday
        };
    }
}