using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using Radzen.Documents.Spreadsheet;

namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Isolated, perf-safe screen-reader announcer for <see cref="RadzenSpreadsheet"/>. It binds to the
/// worksheet <see cref="Selection"/> and, on selection change, re-renders ONLY itself (three visually
/// hidden <c>aria-live</c> regions) - the virtualized cell grid is never touched, so navigation stays
/// on the existing perf-neutral hot path.
/// </summary>
public partial class SpreadsheetAccessibility : ComponentBase, IDisposable
{
    /// <summary>The worksheet whose selection is announced.</summary>
    [Parameter]
    public Worksheet Worksheet { get; set; } = default!;

    /// <summary>The host spreadsheet, used to localize announcement templates.</summary>
    [Parameter]
    public ISpreadsheet Spreadsheet { get; set; } = default!;

    private string? navText;
    private string? actionText;
    private string? alertText;
    private string? lastRange;

    private readonly EventBinding<Selection> selectionBinding;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpreadsheetAccessibility"/> class.
    /// </summary>
    public SpreadsheetAccessibility()
    {
        selectionBinding = new EventBinding<Selection>(
            s => s.Changed += OnSelectionChanged,
            s => s.Changed -= OnSelectionChanged);
    }

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        selectionBinding.Bind(Worksheet?.Selection);
    }

    private void OnSelectionChanged()
    {
        if (Worksheet is null)
        {
            return;
        }

        var changed = false;

        // Active cell (polite nav region) - skip identical consecutive announcements.
        var nav = BuildAnnouncement(Worksheet, Worksheet.Selection.Cell, Localize);
        if (nav != navText)
        {
            navText = nav;
            changed = true;
        }

        // Selection size (separate action region) when a multi-cell range is selected, e.g. "B3:D6, 4 by 3".
        var range = Worksheet.Selection.Range;
        string? rangeText = null;
        if (!range.Collapsed)
        {
            rangeText = string.Format(CultureInfo.CurrentCulture,
                Localize(nameof(RadzenStrings.Spreadsheet_A11ySelection)),
                $"{range.Start}:{range.End}", range.Rows, range.Columns);
        }
        if (rangeText != lastRange)
        {
            lastRange = rangeText;
            actionText = rangeText;
            changed = true;
        }

        if (changed)
        {
            StateHasChanged();
        }
    }

    /// <summary>
    /// Announces a transient action result (sort, filter, insert, undo) in a polite region, keeping
    /// the active cell named so context is not lost.
    /// </summary>
    public void AnnounceAction(string message)
    {
        actionText = message;
        StateHasChanged();
    }

    /// <summary>Announces an assertive alert (for example a validation rejection).</summary>
    public void AnnounceAlert(string message)
    {
        alertText = message;
        StateHasChanged();
    }

    private string Localize(string key) => Spreadsheet?.Localize(key) ?? key;

    /// <summary>
    /// Composes the spoken description of a cell - address, content (or "blank"), header, position,
    /// merged span and state - from the model. Pure and deterministic for unit testing.
    /// </summary>
    /// <param name="worksheet">The worksheet that owns the cell.</param>
    /// <param name="cell">The active cell reference.</param>
    /// <param name="localize">Resolves a <see cref="RadzenStrings"/> key to localized text.</param>
    public static string BuildAnnouncement(Worksheet worksheet, CellRef cell, Func<string, string> localize)
    {
        ArgumentNullException.ThrowIfNull(localize);

        if (worksheet is null || cell == CellRef.Invalid)
        {
            return string.Empty;
        }

        var parts = new List<string>();

        // Content (formatted) or "blank" first - matches the value-then-reference order Excel and
        // Google Sheets use (Excel narrates Value, Name, Context, Properties). The address already
        // conveys the row and column, so we do not also announce "row R of N, column C of M" totals.
        worksheet.Cells.TryGet(cell.Row, cell.Column, out var c);
        var content = GetCellText(c);
        parts.Add(string.IsNullOrEmpty(content)
            ? localize(nameof(RadzenStrings.Spreadsheet_A11yBlank))
            : content);

        // Address ("B3").
        parts.Add(cell.ToString());

        // Header from a frozen header row (suppressed when there is none or it would echo the content).
        var header = GetHeader(worksheet, cell, content);
        if (!string.IsNullOrEmpty(header))
        {
            parts.Add(header);
        }

        // Merged span.
        if (worksheet.MergedCells.Contains(cell))
        {
            parts.Add(localize(nameof(RadzenStrings.Spreadsheet_A11yMerged)));
        }

        // Cell state (all read from the model, no fabricated meaning).
        if (c is not null)
        {
            if (c.Formula is not null)
            {
                parts.Add(localize(nameof(RadzenStrings.Spreadsheet_A11yFormula)));
            }
            if (c.ValueType == CellDataType.Error)
            {
                parts.Add(localize(nameof(RadzenStrings.Spreadsheet_A11yError)));
            }
            if (c.Hyperlink is not null)
            {
                parts.Add(localize(nameof(RadzenStrings.Spreadsheet_A11yHyperlink)));
            }
            if (worksheet.Protection.IsProtected && c.Format.IsLocked)
            {
                parts.Add(localize(nameof(RadzenStrings.Spreadsheet_A11yReadOnly)));
            }
        }
        if (worksheet.FilteredColumns.Contains(cell.Column))
        {
            parts.Add(localize(nameof(RadzenStrings.Spreadsheet_A11yFiltered)));
        }

        return string.Join(", ", parts);
    }

    private static string GetCellText(Cell? c)
    {
        if (c is null || c.IsEmpty)
        {
            return string.Empty;
        }

        return c.GetDisplayText() ?? string.Empty;
    }

    private static string GetHeader(Worksheet worksheet, CellRef cell, string content)
    {
        var frozen = worksheet.Rows.Frozen;
        if (frozen <= 0 || cell.Row < frozen)
        {
            return string.Empty;
        }

        if (worksheet.Cells.TryGet(frozen - 1, cell.Column, out var hc))
        {
            var ht = GetCellText(hc);
            if (!string.IsNullOrEmpty(ht) && ht != content)
            {
                return ht;
            }
        }

        return string.Empty;
    }

    void IDisposable.Dispose()
    {
        selectionBinding.Dispose();
    }
}
