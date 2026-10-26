using System;
using System.Collections.Generic;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Manages per-sheet UI state (SheetView instances) for a workbook.
/// Each sheet gets its own view with independent undo/redo history and rendering state.
/// </summary>
public class WorkbookView
{
    private readonly Dictionary<Worksheet, SheetView> views = [];

    /// <summary>
    /// Gets the workbook this view wraps.
    /// </summary>
    public Workbook Workbook { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkbookView"/> class.
    /// </summary>
    public WorkbookView(Workbook workbook)
    {
        Workbook = workbook ?? throw new ArgumentNullException(nameof(workbook));
    }

    /// <summary>
    /// Gets or creates a SheetView for the specified sheet.
    /// </summary>
    public SheetView GetView(Worksheet sheet)
    {
        ArgumentNullException.ThrowIfNull(sheet);

        if (!views.TryGetValue(sheet, out var view))
        {
            view = new SheetView(sheet);
            ApplyHeaderOffsets(view);
            views[sheet] = view;
        }

        return view;
    }

    private bool showColumnHeaders = true;

    /// <summary>
    /// Gets or sets whether the column headers are rendered. Applies to every existing and future <see cref="SheetView"/>.
    /// </summary>
    public bool ShowColumnHeaders
    {
        get => showColumnHeaders;
        set
        {
            if (showColumnHeaders == value)
            {
                return;
            }

            showColumnHeaders = value;
            ApplyHeaderOffsets();
        }
    }

    private bool showRowHeaders = true;

    /// <summary>
    /// Gets or sets whether the row headers are rendered. Applies to every existing and future <see cref="SheetView"/>.
    /// </summary>
    public bool ShowRowHeaders
    {
        get => showRowHeaders;
        set
        {
            if (showRowHeaders == value)
            {
                return;
            }

            showRowHeaders = value;
            ApplyHeaderOffsets();
        }
    }

    private void ApplyHeaderOffsets()
    {
        foreach (var view in views.Values)
        {
            ApplyHeaderOffsets(view);
        }
    }

    private void ApplyHeaderOffsets(SheetView view)
    {
        view.RowHeaderOffset = showColumnHeaders ? SheetView.DefaultRowHeaderOffset : 0;
        view.ColumnHeaderOffset = showRowHeaders ? SheetView.DefaultColumnHeaderOffset : 0;
    }

    /// <summary>
    /// Removes the view for the specified sheet, freeing its undo history and rendering state.
    /// </summary>
    public bool Remove(Worksheet sheet)
    {
        return views.Remove(sheet);
    }

}
