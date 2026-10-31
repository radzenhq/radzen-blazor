using System;
using System.Collections.Generic;
using System.Linq;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Base class for row and column delete commands. Captures the cells that will be removed and
/// the text of every reference the delete can rewrite, then performs the delete. Undo re-inserts
/// the blank rows/columns and restores the captured cells and references. Snapshot/restore
/// plumbing is inherited from <see cref="RangeSnapshotCommandBase"/>.
/// </summary>
public abstract class DeleteRangeCommandBase : RangeSnapshotCommandBase
{
    /// <summary>
    /// The first index in the range to delete (inclusive).
    /// </summary>
    protected readonly int startIndex;

    /// <summary>
    /// The last index in the range to delete (inclusive).
    /// </summary>
    protected readonly int endIndex;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteRangeCommandBase"/> class.
    /// </summary>
    /// <param name="sheet">The worksheet to modify.</param>
    /// <param name="startIndex">The first row/column index to delete (inclusive).</param>
    /// <param name="endIndex">The last row/column index to delete (inclusive).</param>
    protected DeleteRangeCommandBase(Worksheet sheet, int startIndex, int endIndex)
        : base(sheet)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        this.startIndex = startIndex;
        this.endIndex = endIndex;
    }

    /// <summary>
    /// Gets the number of indices in the deletion range.
    /// </summary>
    protected int Count => endIndex - startIndex + 1;

    private readonly List<(Table Table, TableState State)> tableStates = [];
    private readonly Dictionary<Cell, string> formulas = [];
    private readonly List<(DataValidationRule Rule, string? Formula1, string? Formula2)> validationFormulas = [];
    private readonly List<(ChartSeries Series, string? Categories, string? Values, string? Name)> seriesReferences = [];

    /// <inheritdoc/>
    protected override bool DoExecute()
    {
        tableStates.Clear();

        foreach (var table in sheet.Tables)
        {
            tableStates.Add((table, table.CaptureState()));
        }

        CaptureAffectedCells();
        DeleteRange();
        return true;
    }

    /// <inheritdoc/>
    public override void Unexecute()
    {
        sheet.Batch(() =>
        {
            ReinsertRange();
            RestoreSnapshot();
            RestoreReferences();

            foreach (var (table, state) in tableStates)
            {
                sheet.RestoreTable(table);
                table.RestoreState(state);
            }
        });

        if (tableStates.Count > 0)
        {
            sheet.OnChromeChanged();
        }
    }

    /// <summary>
    /// Returns true when the cell at <paramref name="address"/> falls inside the deletion range.
    /// </summary>
    protected abstract bool IsInsideRange(CellRef address);

    /// <summary>
    /// Deletes the row/column range from the worksheet.
    /// </summary>
    protected abstract void DeleteRange();

    /// <summary>
    /// Re-inserts <see cref="Count"/> blank rows/columns at <see cref="startIndex"/>.
    /// </summary>
    protected abstract void ReinsertRange();

    private void CaptureAffectedCells()
    {
        formulas.Clear();
        validationFormulas.Clear();
        seriesReferences.Clear();

        foreach (var worksheet in sheet.Workbook.Sheets)
        {
            foreach (var cell in worksheet.Cells.GetPopulatedCells().ToList())
            {
                if (worksheet == sheet && IsInsideRange(cell.Address))
                {
                    Capture(cell.Address);
                }
                else if (!string.IsNullOrEmpty(cell.Formula))
                {
                    formulas[cell] = cell.Formula;
                }
            }

            foreach (var series in worksheet.Charts.SelectMany(chart => chart.Series))
            {
                seriesReferences.Add((series, series.Categories, series.Values, series.Name));
            }
        }

        foreach (var rule in sheet.Validation.Ranges.SelectMany(sheet.Validation.GetValidators).OfType<DataValidationRule>().Distinct())
        {
            validationFormulas.Add((rule, rule.Formula1, rule.Formula2));
        }
    }

    private void RestoreReferences()
    {
        foreach (var (cell, formula) in formulas)
        {
            cell.Formula = formula;
        }

        foreach (var (rule, formula1, formula2) in validationFormulas)
        {
            rule.Formula1 = formula1;
            rule.Formula2 = formula2;
        }

        foreach (var (series, categories, values, name) in seriesReferences)
        {
            series.Categories = categories;
            series.Values = values;
            series.Name = name;
        }

        foreach (var worksheet in sheet.Workbook.Sheets.Where(worksheet => worksheet.Charts.Count > 0))
        {
            worksheet.OnChartsChanged();
        }
    }
}
