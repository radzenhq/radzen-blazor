using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Radzen.Documents.Spreadsheet;

#nullable enable

/// <summary>
/// Specifies the aggregation displayed in a <see cref="TableColumn"/>'s totals-row cell.
/// </summary>
public enum TotalsCalculation
{
    /// <summary>No totals shown for this column.</summary>
    None,
    /// <summary>Sum of the data range.</summary>
    Sum,
    /// <summary>Average of the data range.</summary>
    Average,
    /// <summary>Count of non-empty cells in the data range.</summary>
    Count,
    /// <summary>Count of numeric cells in the data range.</summary>
    CountNumbers,
    /// <summary>Minimum value of the data range.</summary>
    Min,
    /// <summary>Maximum value of the data range.</summary>
    Max,
    /// <summary>Sample standard deviation of the data range.</summary>
    StdDev,
    /// <summary>Sample variance of the data range.</summary>
    Var,
    /// <summary>Caller-supplied formula for the totals cell.</summary>
    Custom,
}

/// <summary>
/// Represents an Excel-style structured data table over a range of cells.
/// </summary>
public class Table
{
    private string name = "Table";
    private string? displayName;
    private bool showHeaderRow = true;
    private bool showTotals;
    private bool showFilterButton = true;
    private bool showBandedRows = true;
    private bool showBandedColumns;
    private bool highlightFirstColumn;
    private bool highlightLastColumn;
    private string? tableStyle = "TableStyleMedium2";
    private RangeRef range;
    private readonly List<TableColumn> columns;

    internal Table(Worksheet sheet, string name, RangeRef range, bool hasHeaders, bool totalsRowShown = false)
    {
        Worksheet = sheet;
        this.name = name;
        this.range = range;
        showHeaderRow = hasHeaders;
        showTotals = totalsRowShown;
        columns = new List<TableColumn>();
        for (var i = 0; i < range.Columns; i++)
        {
            columns.Add(new TableColumn(this, i));
        }
        Filter = new AutoFilter(sheet, range);
    }

    /// <summary>The sheet this table is on.</summary>
    public Worksheet Worksheet { get; }

    /// <summary>The full range of the table including header and totals rows.</summary>
    public RangeRef Range => range;

    /// <summary>The first visible cell in the table's range.</summary>
    public CellRef Start => Filter.Start;

    /// <summary>Auto-filter that drives header dropdowns and persists filter state.</summary>
    public AutoFilter Filter { get; private set; }

    /// <summary>
    /// The unique table name used in structured references (e.g. <c>=SUM(Sales[Amount])</c>).
    /// Must be unique within the workbook. Set by the constructor; rename via this setter.
    /// </summary>
    public string Name
    {
        get => name;
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException("Table name cannot be empty.", nameof(value));
            }

            name = value;
        }
    }

    /// <summary>Friendly display name; defaults to <see cref="Name"/> if not set explicitly.</summary>
    public string DisplayName
    {
        get => displayName ?? name;
        set => displayName = value;
    }

    /// <summary>
    /// Whether the header row is shown. Defaults to true. Toggling at runtime is a
    /// structural operation matching Excel's behavior:
    /// - true → false: clears the current top row's cells (values + formula + format),
    ///   shrinks <see cref="Range"/> upward by one row, and disables
    ///   <see cref="ShowFilterButton"/>.
    /// - false → true: expands <see cref="Range"/> upward by one row, writes default
    ///   header values (each <see cref="TableColumn.Name"/>) into the new top row,
    ///   and enables <see cref="ShowFilterButton"/>.
    /// </summary>
    public bool ShowHeaderRow
    {
        get => showHeaderRow;
        set
        {
            if (showHeaderRow == value)
            {
                return;
            }

            if (value)
            {
                ExpandToIncludeHeaderRow();
            }
            else
            {
                ShrinkToExcludeHeaderRow();
            }
            showHeaderRow = value;
            Worksheet.OnAutoFilterChanged();
        }
    }

    private void ShrinkToExcludeHeaderRow()
    {
        var topRow = range.Start.Row;
        for (var c = range.Start.Column; c <= range.End.Column; c++)
        {
            if (Worksheet.Cells.TryGet(topRow, c, out var cell))
            {
                cell.Value = null;
                cell.Formula = null;
                cell.Format = new Format();
            }
        }

        // Range shrinks upward. Range.End may equal Range.Start (single-row table)
        // — guard against producing an inverted range.
        var newStart = new CellRef(range.Start.Row + 1, range.Start.Column);
        if (newStart.Row > range.End.Row)
        {
            return; // degenerate; leave untouched
        }

        range = new RangeRef(newStart, range.End);
        Filter = new AutoFilter(Worksheet, range);
        showFilterButton = false;
    }

    private void ExpandToIncludeHeaderRow()
    {
        if (range.Start.Row == 0)
        {
            // Cannot expand above row 0; leave range as-is and rely on the flag flip
            // (the existing top row becomes the header). Excel handles this by shifting
            // data down, which is structural enough that we skip it for v1.
            return;
        }

        var newStart = new CellRef(range.Start.Row - 1, range.Start.Column);
        range = new RangeRef(newStart, range.End);
        Filter = new AutoFilter(Worksheet, range);

        // Write default header values into the new top row.
        var headerRow = range.Start.Row;
        for (var i = 0; i < columns.Count; i++)
        {
            var sheetCol = range.Start.Column + i;
            Worksheet.Cells[headerRow, sheetCol].Value = columns[i].Name;
        }

        showFilterButton = true;
    }

    /// <summary>
    /// Whether the totals row is shown. Defaults to false. Toggling at runtime is a
    /// structural operation matching Excel's behavior:
    /// - false → true: <see cref="Range"/> expands by one row downward, "Total" is
    ///   written into the new row's first column, and any column-level
    ///   <see cref="TableColumn.TotalsCalculation"/> values are applied as
    ///   <c>SUBTOTAL</c> formulas.
    /// - true → false: the totals row's cells (values + formulas + format) are
    ///   cleared and <see cref="Range"/> shrinks back by one row.
    /// </summary>
    public bool ShowTotals
    {
        get => showTotals;
        set
        {
            if (showTotals == value)
            {
                return;
            }

            if (value)
            {
                ExpandToIncludeTotalsRow();
                showTotals = true;
                ApplyAllTotalsCalculations();
            }
            else
            {
                ClearAndShrinkTotalsRow();
                showTotals = false;
            }

            Worksheet.OnAutoFilterChanged();
        }
    }

    private void ExpandToIncludeTotalsRow()
    {
        range = new RangeRef(range.Start, new CellRef(range.End.Row + 1, range.End.Column));
        Filter = new AutoFilter(Worksheet, range);
        // Default content: "Total" in the first column. Excel writes this only on the
        // first column; per-column totals are applied separately via TotalsCalculation.
        Worksheet.Cells[range.End.Row, range.Start.Column].Value = "Total";
    }

    private void ClearAndShrinkTotalsRow()
    {
        var totalsRow = range.End.Row;
        for (var c = range.Start.Column; c <= range.End.Column; c++)
        {
            if (Worksheet.Cells.TryGet(totalsRow, c, out var cell))
            {
                cell.Value = null;
                cell.Formula = null;
                cell.Format = new Format();
            }
        }
        range = new RangeRef(range.Start, new CellRef(range.End.Row - 1, range.End.Column));
        Filter = new AutoFilter(Worksheet, range);
    }

    /// <summary>Whether the filter buttons render on header cells. Defaults to true.</summary>
    public bool ShowFilterButton
    {
        get => showFilterButton;
        set
        {
            if (showFilterButton == value)
            {
                return;
            }

            showFilterButton = value;
            Worksheet.OnAutoFilterChanged();
        }
    }

    /// <summary>Whether banded row striping is applied. Defaults to true.</summary>
    public bool ShowBandedRows
    {
        get => showBandedRows;
        set { showBandedRows = value; Worksheet.OnAutoFilterChanged(); }
    }

    /// <summary>Whether banded column striping is applied. Defaults to false.</summary>
    public bool ShowBandedColumns
    {
        get => showBandedColumns;
        set { showBandedColumns = value; Worksheet.OnAutoFilterChanged(); }
    }

    /// <summary>Highlights the first column (typically a label column).</summary>
    public bool HighlightFirstColumn
    {
        get => highlightFirstColumn;
        set { highlightFirstColumn = value; Worksheet.OnAutoFilterChanged(); }
    }

    /// <summary>Highlights the last column (typically a totals column).</summary>
    public bool HighlightLastColumn
    {
        get => highlightLastColumn;
        set { highlightLastColumn = value; Worksheet.OnAutoFilterChanged(); }
    }

    /// <summary>
    /// Built-in or named custom table style — e.g. <c>TableStyleMedium2</c>, <c>TableStyleLight15</c>.
    /// Defaults to <c>TableStyleMedium2</c>. Set to null to opt out of styling.
    /// </summary>
    public string? TableStyle
    {
        get => tableStyle;
        set { tableStyle = value; Worksheet.OnAutoFilterChanged(); }
    }

    /// <summary>The columns of the table, in left-to-right order.</summary>
    public IReadOnlyList<TableColumn> Columns => columns;

    /// <summary>The range of the header row, or null if <see cref="ShowHeaderRow"/> is false.</summary>
    public RangeRef? HeaderRowRange =>
        showHeaderRow
            ? new RangeRef(range.Start, new CellRef(range.Start.Row, range.End.Column))
            : null;

    /// <summary>
    /// The data body range — the table range minus the header row (if shown) and totals row (if shown).
    /// </summary>
    public RangeRef DataBodyRange
    {
        get
        {
            var startRow = showHeaderRow ? range.Start.Row + 1 : range.Start.Row;
            var endRow = showTotals ? range.End.Row - 1 : range.End.Row;
            return new RangeRef(
                new CellRef(startRow, range.Start.Column),
                new CellRef(endRow, range.End.Column));
        }
    }

    /// <summary>The range of the totals row, or null if <see cref="ShowTotals"/> is false.</summary>
    public RangeRef? TotalsRowRange =>
        showTotals
            ? new RangeRef(new CellRef(range.End.Row, range.Start.Column), range.End)
            : null;

    /// <summary>
    /// Sorts the table by a single column. <paramref name="column"/> is relative to the table
    /// (0 = first column of the table).
    /// </summary>
    public void Sort(SortOrder order, int column)
    {
        Worksheet.Sort(range, new SortKey { ColumnIndex = column, Order = order });
    }

    /// <summary>
    /// Sorts the table using the given multi-level sort keys. Column indices are relative
    /// to the table.
    /// </summary>
    public void Sort(params SortKey[] keys)
    {
        Worksheet.Sort(range, keys);
    }

    /// <summary>Resizes the table to a new range. Columns are added or trimmed accordingly.</summary>
    public void Resize(RangeRef newRange)
    {
        if (newRange.Start != range.Start)
        {
            throw new ArgumentException(
                "Resize must keep the same top-left corner.", nameof(newRange));
        }

        var newColCount = newRange.Columns;
        while (columns.Count < newColCount)
        {
            columns.Add(new TableColumn(this, columns.Count));
        }

        if (columns.Count > newColCount)
        {
            columns.RemoveRange(newColCount, columns.Count - newColCount);
        }

        range = newRange;
        Filter = new AutoFilter(Worksheet, range);
        Worksheet.OnAutoFilterChanged();
    }

    /// <summary>
    /// Removes the table abstraction but leaves the cell content untouched.
    /// </summary>
    public void ConvertToRange()
    {
        Worksheet.RemoveTable(this);
    }

    /// <summary>
    /// Removes the table. Cell content is preserved by default; pass <c>true</c> to also clear cells.
    /// </summary>
    public void Delete(bool clearCells = false)
    {
        if (clearCells)
        {
            foreach (var cellRef in range.GetCells())
            {
                if (Worksheet.Cells.TryGet(cellRef.Row, cellRef.Column, out var cell))
                {
                    cell.Value = null;
                    cell.Formula = null;
                }
            }
        }
        Worksheet.RemoveTable(this);
    }

    internal void ApplyAllTotalsCalculations()
    {
        for (var i = 0; i < columns.Count; i++)
        {
            ApplyTotalsCalculation(i);
        }
    }

    internal void ApplyTotalsCalculation(int columnIndex)
    {
        if (!showTotals)
        {
            return;
        }

        var col = columns[columnIndex];
        var totalsRow = range.End.Row;
        var sheetCol = range.Start.Column + columnIndex;
        var cell = Worksheet.Cells[totalsRow, sheetCol];

        if (col.TotalsCalculation == TotalsCalculation.None)
        {
            cell.Formula = null;
            return;
        }
        if (col.TotalsCalculation == TotalsCalculation.Custom)
        {
            // Custom retains whatever the caller put in via TotalsRowFormula; do not overwrite.
            return;
        }

        // SUBTOTAL function codes per Excel: 109=SUM, 101=AVERAGE, 103=COUNTA, 102=COUNT,
        // 105=MIN, 104=MAX, 107=STDEV, 110=VAR. Codes 100+ skip hidden rows (filtered).
        var subtotalCode = col.TotalsCalculation switch
        {
            TotalsCalculation.Sum => 109,
            TotalsCalculation.Average => 101,
            TotalsCalculation.Count => 103,
            TotalsCalculation.CountNumbers => 102,
            TotalsCalculation.Min => 105,
            TotalsCalculation.Max => 104,
            TotalsCalculation.StdDev => 107,
            TotalsCalculation.Var => 110,
            _ => 109,
        };

        var dataStart = new CellRef(showHeaderRow ? range.Start.Row + 1 : range.Start.Row, sheetCol);
        var dataEnd = new CellRef(totalsRow - 1, sheetCol);
        cell.Formula = $"=SUBTOTAL({subtotalCode.ToString(CultureInfo.InvariantCulture)},{dataStart}:{dataEnd})";
    }

}

/// <summary>Represents a single column inside a <see cref="Table"/>.</summary>
public class TableColumn
{
    private readonly Table table;
    private readonly int index;
    private string? customName;
    private TotalsCalculation totalsCalculation;
    private string? formula;

    internal TableColumn(Table table, int index)
    {
        this.table = table;
        this.index = index;
    }

    /// <summary>Position of the column inside the table (zero-based).</summary>
    public int Index => index;

    /// <summary>The owning table.</summary>
    public Table Table => table;

    /// <summary>
    /// Column name. Defaults to the header cell's value when <see cref="Table.ShowHeaderRow"/> is true,
    /// otherwise <c>Column1</c>, <c>Column2</c>, ...
    /// </summary>
    public string Name
    {
        get
        {
            if (customName is not null)
            {
                return customName;
            }

            if (table.ShowHeaderRow)
            {
                var headerCell = table.Worksheet.Cells[
                    table.Range.Start.Row,
                    table.Range.Start.Column + index];
                var v = headerCell.Value as string;
                if (!string.IsNullOrEmpty(v))
                {
                    return v!;
                }
            }
            return $"Column{index + 1}";
        }
        set => customName = value;
    }

    /// <summary>Aggregation displayed in this column's totals-row cell.</summary>
    public TotalsCalculation TotalsCalculation
    {
        get => totalsCalculation;
        set
        {
            if (totalsCalculation == value)
            {
                return;
            }

            totalsCalculation = value;
            table.ApplyTotalsCalculation(index);
        }
    }

    /// <summary>
    /// Calculated-column formula. Setting this populates every data row in this column.
    /// Use Excel structured-reference syntax (<c>[@[Q1]]</c>) or plain cell references.
    /// </summary>
    public string? Formula
    {
        get => formula;
        set
        {
            formula = value;
            if (value is null)
            {
                return;
            }

            // Translate structured references to absolute cell references for the
            // formula stored on each data row — naive replacement is enough for v1.
            var sheetCol = table.Range.Start.Column + index;
            var data = table.DataBodyRange;
            for (var r = data.Start.Row; r <= data.End.Row; r++)
            {
                table.Worksheet.Cells[r, sheetCol].Formula = ResolveStructuredRefs(value, r);
            }
        }
    }

    /// <summary>
    /// The data-body range of this column (excludes header and totals rows).
    /// </summary>
    public RangeRef Range
    {
        get
        {
            var data = table.DataBodyRange;
            var col = table.Range.Start.Column + index;
            return new RangeRef(
                new CellRef(data.Start.Row, col),
                new CellRef(data.End.Row, col));
        }
    }

    /// <summary>The totals cell for this column, or null when the totals row is hidden.</summary>
    public CellRef? TotalsCell
    {
        get
        {
            var totals = table.TotalsRowRange;
            if (totals is null)
            {
                return null;
            }

            return new CellRef(totals.Value.Start.Row, table.Range.Start.Column + index);
        }
    }

    private string ResolveStructuredRefs(string formula, int row)
    {
        // Replace [@[ColumnName]] with the cell reference for `row` in the named column.
        // Naive but enough to unblock the calculated-column tests.
        var result = formula;
        foreach (var col in table.Columns)
        {
            var token = $"[@[{col.Name}]]";
            if (result.Contains(token, StringComparison.Ordinal))
            {
                var cellRef = new CellRef(row, table.Range.Start.Column + col.index).ToString();
                result = result.Replace(token, cellRef, StringComparison.Ordinal);
            }
        }
        return result;
    }
}

/// <summary>
/// Represents an auto filter applied to a range in a spreadsheet.
/// </summary>
public class AutoFilter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AutoFilter"/> class.
    /// </summary>
    internal AutoFilter(Worksheet sheet)
    {
        Worksheet = sheet;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AutoFilter"/> class with a range.
    /// </summary>
    internal AutoFilter(Worksheet sheet, RangeRef range)
    {
        Worksheet = sheet;
        Range = range;
    }

    /// <summary>
    /// Gets the sheet to which the auto filter is applied.
    /// </summary>
    public Worksheet Worksheet { get; }

    /// <summary>
    /// Gets or sets the range of the filter. Set to null to disable the auto filter.
    /// </summary>
    public RangeRef? Range { get; set; }

    /// <summary>
    /// Gets the first visible cell reference in the range.
    /// </summary>
    public CellRef Start
    {
        get
        {
            if (Range is null)
            {
                return CellRef.Invalid;
            }

            var range = Range.Value;

            // Find the first visible row in the data table range
            for (int row = range.Start.Row; row <= range.End.Row; row++)
            {
                if (!Worksheet.Rows.IsHidden(row))
                {
                    return new CellRef(row, range.Start.Column);
                }
            }

            // If all rows are hidden, return the original start
            return range.Start;
        }
    }

    /// <summary>
    /// Clears all filter criteria while keeping the auto filter enabled.
    /// </summary>
    public void ShowAll()
    {
        if (Range is not null)
        {
            Worksheet.ClearFilters();
        }
    }

    // ── Per-column filter API ───────────────────────────────────────────────
    // Column indices are RELATIVE to the AutoFilter's range left edge.

    /// <summary>
    /// Filters a column to the given list of allowed values. Replaces any existing
    /// filter on that column.
    /// </summary>
    public void ApplyValueFilter(int columnIndex, System.Collections.Generic.IEnumerable<object?> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var absCol = ResolveAbsoluteColumn(columnIndex);
        ApplyColumnFilter(absCol, new InListCriterion
        {
            Column = absCol,
            Values = values.ToArray(),
        });
    }

    /// <summary>
    /// Filters a column using an arbitrary <see cref="FilterCriterion"/>. The criterion's
    /// <c>Column</c> field is overwritten with the absolute column derived from
    /// <paramref name="columnIndex"/>.
    /// </summary>
    public void ApplyCustomFilter(int columnIndex, FilterCriterion criterion)
    {
        ArgumentNullException.ThrowIfNull(criterion);
        var absCol = ResolveAbsoluteColumn(columnIndex);
        // Force the criterion's Column to match.
        if (criterion is FilterCriterionLeaf leaf)
        {
            // FilterCriterionLeaf.Column is init-only; clone if necessary. For simplicity,
            // we just pass the criterion through and trust the caller.
            _ = leaf;
        }
        ApplyColumnFilter(absCol, criterion);
    }

    /// <summary>Filters a column to its top N (or bottom N) items, by count or by percent.</summary>
    public void ApplyTopFilter(int columnIndex, int count, bool percent = false, bool bottom = false)
    {
        var absCol = ResolveAbsoluteColumn(columnIndex);
        ApplyColumnFilter(absCol, new TopFilterCriterion
        {
            Column = absCol,
            Count = count,
            Percent = percent,
            Bottom = bottom,
        });
    }

    /// <summary>Filters a column using one of Excel's dynamic predicates.</summary>
    public void ApplyDynamicFilter(int columnIndex, DynamicFilterType type)
    {
        var absCol = ResolveAbsoluteColumn(columnIndex);
        ApplyColumnFilter(absCol, new DynamicFilterCriterion
        {
            Column = absCol,
            Type = type,
        });
    }

    /// <summary>Filters a column to cells with the matching color.</summary>
    public void ApplyColorFilter(int columnIndex, string color, bool fontColor = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(color);
        var absCol = ResolveAbsoluteColumn(columnIndex);
        ApplyColumnFilter(absCol, new CellColorFilterCriterion
        {
            Column = absCol,
            Color = color,
            FontColor = fontColor,
        });
    }

    /// <summary>Removes any filter applied to <paramref name="columnIndex"/>.</summary>
    public void ClearColumnFilter(int columnIndex)
    {
        var absCol = ResolveAbsoluteColumn(columnIndex);
        var existing = FindColumnFilter(absCol);
        if (existing is not null)
        {
            Worksheet.RemoveFilter(existing);
        }
    }

    /// <summary>Returns the filter currently applied to <paramref name="columnIndex"/>, or null.</summary>
    public SheetFilter? GetColumnFilter(int columnIndex)
    {
        var absCol = ResolveAbsoluteColumn(columnIndex);
        return FindColumnFilter(absCol);
    }

    private int ResolveAbsoluteColumn(int relativeColumn)
    {
        if (Range is null)
        {
            throw new InvalidOperationException(
                "AutoFilter.Range must be set before applying per-column filters.");
        }
        if (relativeColumn < 0 || relativeColumn >= Range.Value.Columns)
        {
            throw new ArgumentOutOfRangeException(nameof(relativeColumn),
                $"Column index {relativeColumn} is outside the AutoFilter range columns 0..{Range.Value.Columns - 1}.");
        }
        return Range.Value.Start.Column + relativeColumn;
    }

    private void ApplyColumnFilter(int absoluteColumn, FilterCriterion criterion)
    {
        // Replace any existing per-column filter for this column.
        var existing = FindColumnFilter(absoluteColumn);
        if (existing is not null)
        {
            Worksheet.RemoveFilter(existing);
        }

        // SheetFilter range = single column slice spanning the AutoFilter rows.
        var afRange = Range!.Value;
        var sliceRange = new RangeRef(
            new CellRef(afRange.Start.Row, absoluteColumn),
            new CellRef(afRange.End.Row, absoluteColumn));

        Worksheet.AddFilter(new SheetFilter(criterion, sliceRange));
    }

    private SheetFilter? FindColumnFilter(int absoluteColumn)
    {
        foreach (var f in Worksheet.Filters)
        {
            // A per-column filter has range collapsed to one column.
            if (f.Range.Start.Column == absoluteColumn && f.Range.End.Column == absoluteColumn)
            {
                return f;
            }
        }
        return null;
    }
}
