#nullable enable

using System;
using System.Collections.Generic;

namespace Radzen.Documents.Spreadsheet;

static class WorkdayUtil
{
    public static bool IsWeekend(DateTime date) =>
        date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;

    public static HashSet<DateTime> BuildHolidays(List<CellData>? holidays)
    {
        var set = new HashSet<DateTime>();

        if (holidays is null)
        {
            return set;
        }

        foreach (var cell in holidays)
        {
            if (cell.TryCoerceToDate(out var date))
            {
                set.Add(date.Date);
            }
        }

        return set;
    }
}
