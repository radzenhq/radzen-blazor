using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Radzen.Documents.Spreadsheet;

#nullable enable

/// <summary>
/// Excel's dynamic filter operators. Map onto <c>DynamicFilterType</c> in OOXML.
/// </summary>
public enum DynamicFilterType
{
    /// <summary>Above the column's average value.</summary>
    AboveAverage,
    /// <summary>Below the column's average value.</summary>
    BelowAverage,
    /// <summary>Cells whose date equals today.</summary>
    Today,
    /// <summary>Cells whose date equals yesterday.</summary>
    Yesterday,
    /// <summary>Cells whose date equals tomorrow.</summary>
    Tomorrow,
    /// <summary>Cells whose date is in this week (Mon..Sun).</summary>
    ThisWeek,
    /// <summary>Cells whose date is in last week.</summary>
    LastWeek,
    /// <summary>Cells whose date is in next week.</summary>
    NextWeek,
    /// <summary>Cells whose date is in the current calendar month.</summary>
    ThisMonth,
    /// <summary>Cells whose date is in last calendar month.</summary>
    LastMonth,
    /// <summary>Cells whose date is in next calendar month.</summary>
    NextMonth,
    /// <summary>Cells whose date is in the current calendar quarter.</summary>
    ThisQuarter,
    /// <summary>Cells whose date is in last quarter.</summary>
    LastQuarter,
    /// <summary>Cells whose date is in next quarter.</summary>
    NextQuarter,
    /// <summary>Cells whose date is in the current calendar year.</summary>
    ThisYear,
    /// <summary>Cells whose date is in last year.</summary>
    LastYear,
    /// <summary>Cells whose date is in next year.</summary>
    NextYear,
    /// <summary>Cells whose date is between Jan 1 of the current year and today.</summary>
    YearToDate,
    /// <summary>Cells whose date is in January (any year).</summary>
    January,
    /// <summary>Cells whose date is in February (any year).</summary>
    February,
    /// <summary>Cells whose date is in March (any year).</summary>
    March,
    /// <summary>Cells whose date is in April (any year).</summary>
    April,
    /// <summary>Cells whose date is in May (any year).</summary>
    May,
    /// <summary>Cells whose date is in June (any year).</summary>
    June,
    /// <summary>Cells whose date is in July (any year).</summary>
    July,
    /// <summary>Cells whose date is in August (any year).</summary>
    August,
    /// <summary>Cells whose date is in September (any year).</summary>
    September,
    /// <summary>Cells whose date is in October (any year).</summary>
    October,
    /// <summary>Cells whose date is in November (any year).</summary>
    November,
    /// <summary>Cells whose date is in December (any year).</summary>
    December,
    /// <summary>Cells whose date is in Q1 (Jan-Mar) of any year.</summary>
    Quarter1,
    /// <summary>Cells whose date is in Q2 (Apr-Jun) of any year.</summary>
    Quarter2,
    /// <summary>Cells whose date is in Q3 (Jul-Sep) of any year.</summary>
    Quarter3,
    /// <summary>Cells whose date is in Q4 (Oct-Dec) of any year.</summary>
    Quarter4,
}

/// <summary>
/// Filters to the top or bottom N items (or N percent) of a column.
/// </summary>
public class TopFilterCriterion : FilterCriterion
{
    private readonly HashSet<int> matchingRows = [];

    /// <summary>The column this filter applies to.</summary>
    public int Column { get; init; }

    /// <summary>
    /// When <see cref="Percent"/> is false, the number of rows to keep. When true, a 0..100
    /// percentage of the rows.
    /// </summary>
    public int Count { get; init; }

    /// <summary><see cref="Count"/> is interpreted as a percentage instead of a row count.</summary>
    public bool Percent { get; init; }

    /// <summary>When true, keep the lowest values instead of the highest.</summary>
    public bool Bottom { get; init; }

    /// <inheritdoc/>
    public override void OnApply(Worksheet sheet, RangeRef range)
    {
        ArgumentNullException.ThrowIfNull(sheet);

        matchingRows.Clear();

        // Skip header row.
        var startRow = range.Start.Row + 1;
        var endRow = range.End.Row;
        var rowCount = Math.Max(0, endRow - startRow + 1);

        var keep = Percent
            ? (int)Math.Ceiling(rowCount * (Count / 100.0))
            : Count;
        keep = Math.Clamp(keep, 0, rowCount);
        if (keep == 0) return;

        var values = new List<(int row, double value)>(rowCount);
        for (var r = startRow; r <= endRow; r++)
        {
            if (sheet.Cells.TryGet(r, Column, out var cell)
                && TryCoerceToDouble(cell.Value, out var v))
            {
                values.Add((r, v));
            }
        }

        var ordered = Bottom
            ? values.OrderBy(v => v.value)
            : values.OrderByDescending(v => v.value);

        foreach (var (row, _) in ordered.Take(keep))
        {
            matchingRows.Add(row);
        }
    }

    /// <inheritdoc/>
    public override bool Matches(Worksheet sheet, int row) => matchingRows.Contains(row);

    /// <inheritdoc/>
    public override void Accept(IFilterCriterionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    internal static bool TryCoerceToDouble(object? value, out double result)
    {
        switch (value)
        {
            case double d: result = d; return true;
            case int i: result = i; return true;
            case long l: result = l; return true;
            case float f: result = f; return true;
            case decimal m: result = (double)m; return true;
            case DateTime dt: result = dt.ToOADate(); return true;
            case string s when double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var n):
                result = n; return true;
        }
        result = default;
        return false;
    }
}

/// <summary>
/// Filters using one of Excel's built-in dynamic predicates (above-average, today, this-month, etc.).
/// </summary>
public class DynamicFilterCriterion : FilterCriterion
{
    private double average;
    private bool averageComputed;

    /// <summary>The column this filter applies to.</summary>
    public int Column { get; init; }

    /// <summary>The dynamic predicate to apply.</summary>
    public DynamicFilterType Type { get; init; }

    /// <inheritdoc/>
    public override void OnApply(Worksheet sheet, RangeRef range)
    {
        ArgumentNullException.ThrowIfNull(sheet);

        averageComputed = false;
        if (Type is DynamicFilterType.AboveAverage or DynamicFilterType.BelowAverage)
        {
            var sum = 0.0;
            var n = 0;
            for (var r = range.Start.Row + 1; r <= range.End.Row; r++)
            {
                if (sheet.Cells.TryGet(r, Column, out var cell)
                    && TopFilterCriterion.TryCoerceToDouble(cell.Value, out var v))
                {
                    sum += v;
                    n++;
                }
            }
            if (n > 0)
            {
                average = sum / n;
                averageComputed = true;
            }
        }
    }

    /// <inheritdoc/>
    public override bool Matches(Worksheet sheet, int row)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        var cell = sheet.Cells[row, Column];

        switch (Type)
        {
            case DynamicFilterType.AboveAverage:
                if (!averageComputed) return false;
                return TopFilterCriterion.TryCoerceToDouble(cell.Value, out var av) && av > average;

            case DynamicFilterType.BelowAverage:
                if (!averageComputed) return false;
                return TopFilterCriterion.TryCoerceToDouble(cell.Value, out var bv) && bv < average;

            default:
                return TryGetDate(cell.Value, out var date) && MatchesDate(date);
        }
    }

    private bool MatchesDate(DateTime date)
    {
        var today = DateTime.Today;
        var d = date.Date;

        return Type switch
        {
            DynamicFilterType.Today => d == today,
            DynamicFilterType.Yesterday => d == today.AddDays(-1),
            DynamicFilterType.Tomorrow => d == today.AddDays(1),
            DynamicFilterType.ThisWeek => InWeek(d, today),
            DynamicFilterType.LastWeek => InWeek(d, today.AddDays(-7)),
            DynamicFilterType.NextWeek => InWeek(d, today.AddDays(7)),
            DynamicFilterType.ThisMonth => d.Year == today.Year && d.Month == today.Month,
            DynamicFilterType.LastMonth => InMonth(d, today.AddMonths(-1)),
            DynamicFilterType.NextMonth => InMonth(d, today.AddMonths(1)),
            DynamicFilterType.ThisQuarter => InQuarter(d, today),
            DynamicFilterType.LastQuarter => InQuarter(d, today.AddMonths(-3)),
            DynamicFilterType.NextQuarter => InQuarter(d, today.AddMonths(3)),
            DynamicFilterType.ThisYear => d.Year == today.Year,
            DynamicFilterType.LastYear => d.Year == today.Year - 1,
            DynamicFilterType.NextYear => d.Year == today.Year + 1,
            DynamicFilterType.YearToDate => d.Year == today.Year && d <= today,
            DynamicFilterType.January => d.Month == 1,
            DynamicFilterType.February => d.Month == 2,
            DynamicFilterType.March => d.Month == 3,
            DynamicFilterType.April => d.Month == 4,
            DynamicFilterType.May => d.Month == 5,
            DynamicFilterType.June => d.Month == 6,
            DynamicFilterType.July => d.Month == 7,
            DynamicFilterType.August => d.Month == 8,
            DynamicFilterType.September => d.Month == 9,
            DynamicFilterType.October => d.Month == 10,
            DynamicFilterType.November => d.Month == 11,
            DynamicFilterType.December => d.Month == 12,
            DynamicFilterType.Quarter1 => d.Month is >= 1 and <= 3,
            DynamicFilterType.Quarter2 => d.Month is >= 4 and <= 6,
            DynamicFilterType.Quarter3 => d.Month is >= 7 and <= 9,
            DynamicFilterType.Quarter4 => d.Month is >= 10 and <= 12,
            _ => false,
        };
    }

    private static bool InWeek(DateTime d, DateTime reference)
    {
        // ISO 8601: weeks start Monday.
        var refMonday = reference.AddDays(-((int)reference.DayOfWeek + 6) % 7);
        var refSunday = refMonday.AddDays(7);
        return d >= refMonday && d < refSunday;
    }

    private static bool InMonth(DateTime d, DateTime reference) =>
        d.Year == reference.Year && d.Month == reference.Month;

    private static bool InQuarter(DateTime d, DateTime reference)
    {
        if (d.Year != reference.Year) return false;
        var quarterOf = (int month) => (month - 1) / 3 + 1;
        return quarterOf(d.Month) == quarterOf(reference.Month);
    }

    private static bool TryGetDate(object? value, out DateTime date)
    {
        switch (value)
        {
            case DateTime dt:
                date = dt;
                return true;
            case double d:
                try { date = DateTime.FromOADate(d); return true; }
                catch { date = default; return false; }
            case string s when DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed):
                date = parsed;
                return true;
        }
        date = default;
        return false;
    }

    /// <inheritdoc/>
    public override void Accept(IFilterCriterionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }
}

/// <summary>
/// Filters to cells whose background or font color matches a target color.
/// </summary>
public class CellColorFilterCriterion : FilterCriterion
{
    /// <summary>The column this filter applies to.</summary>
    public int Column { get; init; }

    /// <summary>The hex color string to match (e.g. <c>#FFFF00</c>).</summary>
    public string? Color { get; init; }

    /// <summary>
    /// When false (default), matches against the cell's <see cref="Format.BackgroundColor"/>.
    /// When true, matches against <see cref="Format.Color"/> (font color).
    /// </summary>
    public bool FontColor { get; init; }

    /// <inheritdoc/>
    public override bool Matches(Worksheet sheet, int row)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        if (!sheet.Cells.TryGet(row, Column, out var cell)) return false;
        var actual = FontColor ? cell.Format.Color : cell.Format.BackgroundColor;
        return string.Equals(actual, Color, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
    public override void Accept(IFilterCriterionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }
}
