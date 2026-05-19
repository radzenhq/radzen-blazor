using System;

namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Event arguments passed to <see cref="Radzen.Blazor.RadzenSpreadsheet"/>'s
/// <c>CommandExecuting</c> callback. The handler may call <see cref="PreventDefault"/>
/// to veto the command before it is pushed onto the undo stack.
/// </summary>
public class SpreadsheetCommandEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of <see cref="SpreadsheetCommandEventArgs"/>.
    /// </summary>
    public SpreadsheetCommandEventArgs(ICommand command)
    {
        Command = command;
    }

    /// <summary>
    /// The command about to be executed.
    /// </summary>
    public ICommand Command { get; }

    /// <summary>
    /// Returns <c>true</c> once <see cref="PreventDefault"/> has been called.
    /// </summary>
    public bool DefaultPrevented { get; private set; }

    /// <summary>
    /// Prevents the command from running. The host's <c>Execute</c> will return
    /// <c>false</c>. Must be called synchronously from the handler — async handlers that
    /// only call this after an <c>await</c> will not gate the command.
    /// </summary>
    public void PreventDefault() => DefaultPrevented = true;
}
