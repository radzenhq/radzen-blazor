#nullable enable

using System;

namespace Radzen.Documents.Spreadsheet;

class WorkdayFunction : FormulaFunction
{
    public override string Name => "WORKDAY";

    public override FunctionParameter[] Parameters =>
    [
        new("start_date", ParameterType.Single, isRequired: true),
        new("days", ParameterType.Single, isRequired: true),
        new("holidays", ParameterType.Collection, isRequired: false)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var start = arguments.GetSingle("start_date");

        if (start is null)
        {
            return CellData.FromError(CellError.Value);
        }

        if (!start.TryCoerceToDate(out var startDate))
        {
            return CellData.FromError(CellError.Value);
        }

        if (!TryGetInteger(arguments, "days", isRequired: true, defaultValue: null, out var days, out var daysError))
        {
            return daysError!;
        }

        var holidays = WorkdayUtil.BuildHolidays(arguments.GetRange("holidays"));

        var step = days >= 0 ? 1 : -1;
        var remaining = Math.Abs(days);
        var current = startDate.Date;

        try
        {
            while (remaining > 0)
            {
                current = current.AddDays(step);

                if (!WorkdayUtil.IsWeekend(current) && !holidays.Contains(current))
                {
                    remaining--;
                }
            }
        }
        catch (ArgumentOutOfRangeException)
        {
            return CellData.FromError(CellError.Num);
        }

        return CellData.FromDate(current);
    }
}
