#nullable enable

using Radzen.Documents.Spreadsheet;
using System;
using System.Collections.Generic;
namespace Radzen.Blazor.Spreadsheet;

enum AutofillDirection
{
    Down,
    Up,
    Right,
    Left
}

class AutofillCommand : RangeSnapshotCommandBase
{
    public override SheetAction RequiredAction => SheetAction.EditCell;

    public override SpreadsheetFeature? Feature => SpreadsheetFeature.Autofill;

    internal static RangeRef ComputeRange(RangeRef source, CellRef target)
    {
        var rowDelta = 0;
        var colDelta = 0;

        if (target.Row > source.End.Row)
        {
            rowDelta = target.Row - source.End.Row;
        }
        else if (target.Row < source.Start.Row)
        {
            rowDelta = target.Row - source.Start.Row;
        }

        if (target.Column > source.End.Column)
        {
            colDelta = target.Column - source.End.Column;
        }
        else if (target.Column < source.Start.Column)
        {
            colDelta = target.Column - source.Start.Column;
        }

        if (Math.Abs(rowDelta) >= Math.Abs(colDelta))
        {
            if (rowDelta > 0)
            {
                return new RangeRef(source.Start, new CellRef(target.Row, source.End.Column));
            }

            if (rowDelta < 0)
            {
                return new RangeRef(new CellRef(target.Row, source.Start.Column), source.End);
            }
        }
        else
        {
            if (colDelta > 0)
            {
                return new RangeRef(source.Start, new CellRef(source.End.Row, target.Column));
            }

            if (colDelta < 0)
            {
                return new RangeRef(new CellRef(source.Start.Row, target.Column), source.End);
            }
        }

        return RangeRef.Invalid;
    }

    internal static AutofillDirection GetDirection(RangeRef source, RangeRef fill)
    {
        if (fill.End.Row > source.End.Row)
        {
            return AutofillDirection.Down;
        }

        if (fill.Start.Row < source.Start.Row)
        {
            return AutofillDirection.Up;
        }

        if (fill.End.Column > source.End.Column)
        {
            return AutofillDirection.Right;
        }

        return AutofillDirection.Left;
    }

    private readonly RangeRef source;
    private readonly RangeRef target;
    private readonly AutofillDirection direction;

    public AutofillCommand(Worksheet sheet, RangeRef source, RangeRef target, AutofillDirection direction)
        : base(sheet)
    {
        this.source = source;
        this.target = target;
        this.direction = direction;
    }

    protected override bool DoExecute()
    {
        if (source == RangeRef.Invalid || target == RangeRef.Invalid)
        {
            return false;
        }

        for (var row = target.Start.Row; row <= target.End.Row; row++)
        {
            for (var column = target.Start.Column; column <= target.End.Column; column++)
            {
                if (source.Contains(row, column))
                {
                    continue;
                }

                Capture(new CellRef(row, column));
            }
        }

        if (snapshot.Count == 0)
        {
            return false;
        }

        sheet.Batch(Fill);

        NotifyChanged();

        return true;
    }

    private void Fill()
    {
        if (direction == AutofillDirection.Down || direction == AutofillDirection.Up)
        {
            FillVertical();
        }
        else
        {
            FillHorizontal();
        }
    }

    private void NotifyChanged()
    {
        foreach (var cellRef in snapshot.Keys)
        {
            if (sheet.Cells.TryGet(cellRef.Row, cellRef.Column, out var cell))
            {
                cell.OnChanged();
            }
        }
    }

    public override void Unexecute()
    {
        sheet.Batch(RestoreSnapshot);

        NotifyChanged();
    }

    private void FillVertical()
    {
        var sourceRows = source.End.Row - source.Start.Row + 1;

        int startRow, endRow, step;

        if (direction == AutofillDirection.Down)
        {
            startRow = source.End.Row + 1;
            endRow = target.End.Row;
            step = 1;
        }
        else
        {
            startRow = source.Start.Row - 1;
            endRow = target.Start.Row;
            step = -1;
        }

        for (var column = source.Start.Column; column <= source.End.Column; column++)
        {
            var sourceValues = new List<object?>();
            var sourceFormulas = new List<string?>();
            var sourceFormats = new List<Format?>();

            for (var sr = source.Start.Row; sr <= source.End.Row; sr++)
            {
                if (sheet.Cells.TryGet(sr, column, out var srcCell))
                {
                    sourceValues.Add(srcCell.Value);
                    sourceFormulas.Add(srcCell.Formula);
                    sourceFormats.Add(srcCell.Format?.Clone());
                }
                else
                {
                    sourceValues.Add(null);
                    sourceFormulas.Add(null);
                    sourceFormats.Add(null);
                }
            }

            var series = DetectSeries(sourceValues);

            var fillIndex = 0;

            for (var row = startRow; step > 0 ? row <= endRow : row >= endRow; row += step)
            {
                var sourceIndex = fillIndex % sourceRows;
                var sourceRow = source.Start.Row + sourceIndex;
                var rowDelta = row - sourceRow;

                var dstCell = sheet.Cells[row, column];

                if (sourceFormats[sourceIndex] is not null)
                {
                    dstCell.Format = sourceFormats[sourceIndex]!.Clone();
                }

                if (!string.IsNullOrEmpty(sourceFormulas[sourceIndex]))
                {
                    var adjusted = Worksheet.AdjustFormulaForCopy(sourceFormulas[sourceIndex]!, rowDelta, 0);
                    dstCell.Formula = adjusted;
                }
                else if (series is not null)
                {
                    var n = fillIndex + 1;
                    dstCell.Value = series.GetValue(n, direction);
                }
                else
                {
                    dstCell.Value = sourceValues[sourceIndex];
                }

                fillIndex++;
            }
        }
    }

    private void FillHorizontal()
    {
        var sourceCols = source.End.Column - source.Start.Column + 1;

        int startCol, endCol, step;

        if (direction == AutofillDirection.Right)
        {
            startCol = source.End.Column + 1;
            endCol = target.End.Column;
            step = 1;
        }
        else
        {
            startCol = source.Start.Column - 1;
            endCol = target.Start.Column;
            step = -1;
        }

        for (var row = source.Start.Row; row <= source.End.Row; row++)
        {
            var sourceValues = new List<object?>();
            var sourceFormulas = new List<string?>();
            var sourceFormats = new List<Format?>();

            for (var sc = source.Start.Column; sc <= source.End.Column; sc++)
            {
                if (sheet.Cells.TryGet(row, sc, out var srcCell))
                {
                    sourceValues.Add(srcCell.Value);
                    sourceFormulas.Add(srcCell.Formula);
                    sourceFormats.Add(srcCell.Format?.Clone());
                }
                else
                {
                    sourceValues.Add(null);
                    sourceFormulas.Add(null);
                    sourceFormats.Add(null);
                }
            }

            var series = DetectSeries(sourceValues);

            var fillIndex = 0;

            for (var column = startCol; step > 0 ? column <= endCol : column >= endCol; column += step)
            {
                var sourceIndex = fillIndex % sourceCols;
                var sourceColumn = source.Start.Column + sourceIndex;
                var colDelta = column - sourceColumn;

                var dstCell = sheet.Cells[row, column];

                if (sourceFormats[sourceIndex] is not null)
                {
                    dstCell.Format = sourceFormats[sourceIndex]!.Clone();
                }

                if (!string.IsNullOrEmpty(sourceFormulas[sourceIndex]))
                {
                    var adjusted = Worksheet.AdjustFormulaForCopy(sourceFormulas[sourceIndex]!, 0, colDelta);
                    dstCell.Formula = adjusted;
                }
                else if (series is not null)
                {
                    var n = fillIndex + 1;
                    dstCell.Value = series.GetValue(n, direction);
                }
                else
                {
                    dstCell.Value = sourceValues[sourceIndex];
                }

                fillIndex++;
            }
        }
    }

    // Tolerance for numeric arithmetic-progression detection; loose enough to absorb accumulated floating-point error, tight enough to reject deliberately uneven steps.
    private const double NumericStepEpsilon = 1e-7;

    // Tolerance for date-step equality, expressed in days (~86 ms); tight enough to distinguish "off by a second" series, loose enough for round-trips through ToOADate/double.
    private const double DateStepEpsilonInDays = 1e-6;

    private static SeriesInfo? DetectSeries(List<object?> sourceValues)
    {
        if (sourceValues.Count < 2)
        {
            return null;
        }

        var numbers = new List<double>();

        foreach (var v in sourceValues)
        {
            if (v is double d)
            {
                numbers.Add(d);
            }
            else
            {
                numbers.Clear();
                break;
            }
        }

        if (numbers.Count >= 2)
        {
            var diff = numbers[1] - numbers[0];
            var isArithmetic = true;

            for (var i = 2; i < numbers.Count; i++)
            {
                if (Math.Abs((numbers[i] - numbers[i - 1]) - diff) > NumericStepEpsilon)
                {
                    isArithmetic = false;
                    break;
                }
            }

            if (isArithmetic)
            {
                return new NumericSeries(numbers[numbers.Count - 1], numbers[0], diff);
            }
        }

        var dates = new List<DateTime>();

        foreach (var v in sourceValues)
        {
            if (v is DateTime dt)
            {
                dates.Add(dt);
            }
            else
            {
                dates.Clear();
                break;
            }
        }

        if (dates.Count >= 2)
        {
            var dayDiff = (dates[1] - dates[0]).TotalDays;
            var isConstant = true;

            for (var i = 2; i < dates.Count; i++)
            {
                if (Math.Abs((dates[i] - dates[i - 1]).TotalDays - dayDiff) > DateStepEpsilonInDays)
                {
                    isConstant = false;
                    break;
                }
            }

            if (isConstant)
            {
                return new DateSeries(dates[dates.Count - 1], dates[0], dayDiff);
            }
        }

        return null;
    }

    private abstract class SeriesInfo
    {
        public abstract object GetValue(int n, AutofillDirection direction);
    }

    private sealed class NumericSeries(double last, double first, double diff) : SeriesInfo
    {
        public override object GetValue(int n, AutofillDirection direction)
        {
            return (direction == AutofillDirection.Down || direction == AutofillDirection.Right)
                ? last + diff * n
                : first - diff * n;
        }
    }

    private sealed class DateSeries(DateTime last, DateTime first, double dayDiff) : SeriesInfo
    {
        public override object GetValue(int n, AutofillDirection direction)
        {
            return (direction == AutofillDirection.Down || direction == AutofillDirection.Right)
                ? last.AddDays(dayDiff * n)
                : first.AddDays(-dayDiff * n);
        }
    }
}
