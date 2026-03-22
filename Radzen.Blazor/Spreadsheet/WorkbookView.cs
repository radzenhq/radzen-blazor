using System;
using System.Collections.Generic;

namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Manages per-sheet UI state (SheetView instances) for a workbook.
/// Each sheet gets its own view with independent undo/redo history and rendering state.
/// </summary>
public class WorkbookView
{
    private readonly Dictionary<Sheet, SheetView> views = [];

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
    public SheetView GetView(Sheet sheet)
    {
        ArgumentNullException.ThrowIfNull(sheet);

        if (!views.TryGetValue(sheet, out var view))
        {
            view = new SheetView(sheet);
            views[sheet] = view;
        }

        return view;
    }

    /// <summary>
    /// Removes the view for the specified sheet, freeing its undo history and rendering state.
    /// </summary>
    public bool Remove(Sheet sheet)
    {
        return views.Remove(sheet);
    }

}
