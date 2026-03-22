#nullable enable

using System;

namespace Radzen.Documents.Spreadsheet;

class DatedifFunction : FormulaFunction
{
    public override string Name => "DATEDIF";

    public override FunctionParameter[] Parameters =>
    [
        new("start_date", ParameterType.Single, isRequired: true),
        new("end_date", ParameterType.Single, isRequired: true),
        new("unit", ParameterType.Single, isRequired: true)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var startArg = arguments.GetSingle("start_date");
        if (startArg == null || startArg.IsError)
        {
            return startArg ?? CellData.FromError(CellError.Value);
        }

        if (!startArg.TryCoerceToDate(out var startDate))
        {
            return CellData.FromError(CellError.Value);
        }

        var endArg = arguments.GetSingle("end_date");
        if (endArg == null || endArg.IsError)
        {
            return endArg ?? CellData.FromError(CellError.Value);
        }

        if (!endArg.TryCoerceToDate(out var endDate))
        {
            return CellData.FromError(CellError.Value);
        }

        if (!TryGetString(arguments, "unit", out var unit, out var error))
        {
            return error!;
        }

        if (startDate > endDate)
        {
            return CellData.FromError(CellError.Num);
        }

        return unit.ToUpperInvariant() switch
        {
            "Y" => CellData.FromNumber(FullYears(startDate, endDate)),
            "M" => CellData.FromNumber(FullMonths(startDate, endDate)),
            "D" => CellData.FromNumber((endDate - startDate).Days),
            "YM" => CellData.FromNumber(FullMonths(startDate, endDate) % 12),
            "MD" => CellData.FromNumber(DaysIgnoringMonths(startDate, endDate)),
            "YD" => CellData.FromNumber(DaysIgnoringYears(startDate, endDate)),
            _ => CellData.FromError(CellError.Num)
        };
    }

    private static int FullYears(DateTime start, DateTime end)
    {
        var years = end.Year - start.Year;
        if (end.Month < start.Month || (end.Month == start.Month && end.Day < start.Day))
        {
            years--;
        }
        return years;
    }

    private static int FullMonths(DateTime start, DateTime end)
    {
        var months = (end.Year - start.Year) * 12 + end.Month - start.Month;
        if (end.Day < start.Day)
        {
            months--;
        }
        return months;
    }

    private static int DaysIgnoringMonths(DateTime start, DateTime end)
    {
        var day = end.Day - start.Day;
        if (day < 0)
        {
            var prevMonth = end.AddMonths(-1);
            day = DateTime.DaysInMonth(prevMonth.Year, prevMonth.Month) - start.Day + end.Day;
        }
        return day;
    }

    private static int DaysIgnoringYears(DateTime start, DateTime end)
    {
        var adjustedEnd = new DateTime(start.Year, end.Month, end.Day);
        if (adjustedEnd < start)
        {
            adjustedEnd = new DateTime(start.Year + 1, end.Month, end.Day);
        }
        return (adjustedEnd - start).Days;
    }
}
