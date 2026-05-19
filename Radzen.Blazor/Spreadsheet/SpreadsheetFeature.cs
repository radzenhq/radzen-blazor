namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Enumerates feature areas that can be toggled on or off via the
/// <c>Allow*</c> parameters on <see cref="Radzen.Blazor.RadzenSpreadsheet"/>.
/// </summary>
public enum SpreadsheetFeature
{
    /// <summary>Direct cell editing (typing, paste-into-cell, clearing contents, autoaccept of edits, row/column insert/delete).</summary>
    Editing,

    /// <summary>Auto-filter and column-filter operations.</summary>
    Filtering,

    /// <summary>Single- and multi-key sort operations.</summary>
    Sorting,

    /// <summary>Drag-to-fill (autofill) gestures and the resulting fill command.</summary>
    Autofill,

    /// <summary>Cell-merge and unmerge operations.</summary>
    Merging,

    /// <summary>Row and column resize gestures.</summary>
    Resizing,

    /// <summary>Format-cell operations such as font, color, alignment, and borders.</summary>
    CellFormatting,

    /// <summary>Inserting, editing, and following hyperlinks.</summary>
    Hyperlinks,

    /// <summary>Inserting, moving, resizing, and deleting images.</summary>
    Images,

    /// <summary>Inserting, editing, moving, resizing, and deleting charts.</summary>
    Charts,

    /// <summary>Creating, editing, and removing structured tables.</summary>
    Tables,

    /// <summary>Adding and clearing data-validation rules.</summary>
    DataValidation,

    /// <summary>Adding and clearing conditional formatting rules.</summary>
    ConditionalFormatting,

    /// <summary>Cut, copy, and paste through the system clipboard.</summary>
    Clipboard,

    /// <summary>Undo and redo through the command history.</summary>
    UndoRedo,
}
