#nullable enable

namespace Radzen.Documents.Spreadsheet;

class NetworkDaysFunction : FormulaFunction
{
    public override string Name => "NETWORKDAYS";

    public override FunctionParameter[] Parameters =>
    [
        new("start_date", ParameterType.Single, isRequired: true),
        new("end_date", ParameterType.Single, isRequired: true),
        new("holidays", ParameterType.Collection, isRequired: false)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var start = arguments.GetSingle("start_date");
        var end = arguments.GetSingle("end_date");

        if (start is null || end is null)
        {
            return CellData.FromError(CellError.Value);
        }

        if (!start.TryCoerceToDate(out var startDate) || !end.TryCoerceToDate(out var endDate))
        {
            return CellData.FromError(CellError.Value);
        }

        var holidays = WorkdayUtil.BuildHolidays(arguments.GetRange("holidays"));

        var from = startDate.Date;
        var to = endDate.Date;
        var sign = 1;

        if (from > to)
        {
            (from, to) = (to, from);
            sign = -1;
        }

        var count = 0;

        for (var day = from; day <= to; day = day.AddDays(1))
        {
            if (!WorkdayUtil.IsWeekend(day) && !holidays.Contains(day))
            {
                count++;
            }
        }

        return CellData.FromNumber(count * sign);
    }
}
