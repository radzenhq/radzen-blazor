using System;
using Radzen.Documents.Spreadsheet;

namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Describes what caused <see cref="Radzen.Blazor.RadzenSpreadsheet"/>'s <c>Change</c>
/// callback to fire.
/// </summary>
public enum SpreadsheetChangeReason
{
    /// <summary>A command mutated the worksheet.</summary>
    Command,
    /// <summary>A previously executed command was undone.</summary>
    Undo,
    /// <summary>A previously undone command was redone.</summary>
    Redo,
    /// <summary>A sheet was added to the workbook.</summary>
    SheetAdded,
    /// <summary>A sheet was removed from the workbook.</summary>
    SheetRemoved,
    /// <summary>A sheet was renamed.</summary>
    SheetRenamed,
    /// <summary>A sheet was moved to a different position.</summary>
    SheetMoved
}

/// <summary>
/// Event arguments passed to <see cref="Radzen.Blazor.RadzenSpreadsheet"/>'s
/// <c>Change</c> callback after the workbook has been mutated.
/// </summary>
public class SpreadsheetChangeEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of <see cref="SpreadsheetChangeEventArgs"/>.
    /// </summary>
    public SpreadsheetChangeEventArgs(SpreadsheetChangeReason reason, Worksheet? worksheet, ICommand? command = null)
    {
        Reason = reason;
        Worksheet = worksheet;
        Command = command;
    }

    /// <summary>
    /// What caused the change.
    /// </summary>
    public SpreadsheetChangeReason Reason { get; }

    /// <summary>
    /// The worksheet the change applies to.
    /// </summary>
    public Worksheet? Worksheet { get; }

    /// <summary>
    /// The command that ran, was undone, or was redone. <c>null</c> for sheet
    /// management changes, which do not go through the undo stack.
    /// </summary>
    public ICommand? Command { get; }
}
