#nullable enable

using System;
using System.Collections.Generic;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Specifies the direction of an autofill operation.
/// </summary>
enum AutofillDirection
{
    Down,
    Up,
    Right,
    Left
}

/// <summary>
/// Command that fills a target range by repeating a source range pattern.
/// Supports numeric series, date series, formula adjustment, and plain copy.
/// </summary>
class AutofillCommand : ICommand, IProtectedCommand
{
    /// <inheritdoc/>
    public SheetAction RequiredAction => SheetAction.EditCell;

    /// <summary>
    /// Computes the autofill target range from the source selection and the cell the user dragged to.
    /// Constrains to the dominant axis (vertical or horizontal).
    /// </summary>
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

    /// <summary>
    /// Determines the fill direction by comparing the fill range to the source range.
    /// </summary>
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

    private readonly Worksheet sheet;
    private readonly RangeRef source;
    private readonly RangeRef target;
    private readonly AutofillDirection direction;
    private readonly Dictionary<(int row, int column), (object? value, string? formula, Format? format)> snapshot = [];

    public AutofillCommand(Worksheet sheet, RangeRef source, RangeRef target, AutofillDirection direction)
    {
        this.sheet = sheet ?? throw new ArgumentNullException(nameof(sheet));
        this.source = source;
        this.target = target;
        this.direction = direction;
    }

    public bool Execute()
    {
        if (source == RangeRef.Invalid || target == RangeRef.Invalid)
        {
            return false;
        }

        // Snapshot target cells for undo
        for (var row = target.Start.Row; row <= target.End.Row; row++)
        {
            for (var column = target.Start.Column; column <= target.End.Column; column++)
            {
                if (source.Contains(row, column))
                {
                    continue;
                }

                object? value = null;
                string? formula = null;
                Format? format = null;

                if (sheet.Cells.TryGet(row, column, out var cell))
                {
                    value = cell.Value;
                    formula = cell.Formula;
                    format = cell.Format?.Clone();
                }

                snapshot[(row, column)] = (value, formula, format);
            }
        }

        if (snapshot.Count == 0)
        {
            return false;
        }

        sheet.BeginUpdate();

        if (direction == AutofillDirection.Down || direction == AutofillDirection.Up)
        {
            FillVertical();
        }
        else
        {
            FillHorizontal();
        }

        sheet.EndUpdate();

        foreach (var (row, column) in snapshot.Keys)
        {
            if (sheet.Cells.TryGet(row, column, out var cell))
            {
                cell.OnChanged();
            }
        }

        return true;
    }

    public void Unexecute()
    {
        sheet.BeginUpdate();

        foreach (var ((row, column), (value, formula, format)) in snapshot)
        {
            var cell = sheet.Cells[row, column];

            if (formula is not null)
            {
                cell.Formula = formula;
            }
            else
            {
                cell.Formula = null;
                cell.Value = value;
            }

            if (format is not null)
            {
                cell.Format = format;
            }
        }

        sheet.EndUpdate();

        foreach (var (row, column) in snapshot.Keys)
        {
            if (sheet.Cells.TryGet(row, column, out var cell))
            {
                cell.OnChanged();
            }
        }
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

            // Detect series once per column
            var series = DetectSeries(sourceValues);

            var fillIndex = 0;

            for (var row = startRow; step > 0 ? row <= endRow : row >= endRow; row += step)
            {
                var sourceIndex = fillIndex % sourceRows;
                var sourceRow = source.Start.Row + sourceIndex;
                var rowDelta = row - sourceRow;

                var dstCell = sheet.Cells[row, column];

                // Copy format
                if (sourceFormats[sourceIndex] is not null)
                {
                    dstCell.Format = sourceFormats[sourceIndex]!.Clone();
                }

                // Handle formula
                if (!string.IsNullOrEmpty(sourceFormulas[sourceIndex]))
                {
                    var adjusted = Worksheet.AdjustFormulaForCopy(sourceFormulas[sourceIndex]!, rowDelta, 0);
                    dstCell.Formula = adjusted;
                }
                else if (series is not null)
                {
                    // Continue the series from the end (or start for reverse)
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

    /// <summary>
    /// Attempts to detect an arithmetic numeric or date series in the source values.
    /// Returns null if no series pattern is detected.
    /// </summary>
    private static SeriesInfo? DetectSeries(List<object?> sourceValues)
    {
        if (sourceValues.Count < 2)
        {
            return null;
        }

        // Try numeric series — CellData stores all numbers as double
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
                if (Math.Abs((numbers[i] - numbers[i - 1]) - diff) > 1e-10)
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

        // Try date series
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
                if (Math.Abs((dates[i] - dates[i - 1]).TotalDays - dayDiff) > 0.001)
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
        /// <summary>
        /// Gets the nth fill value. n is 1-based (1 = first cell past the source boundary).
        /// </summary>
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
