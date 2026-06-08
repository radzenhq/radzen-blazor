using System;
using System.Collections.Generic;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

#nullable enable
/// <summary>
/// Represents a command that can be executed, undone, and redone.
/// </summary>
public interface ICommand
{
    /// <summary>
    /// Executes the command.
    /// Returns true if the command was successfully executed, false otherwise.
    /// </summary>
    /// <returns></returns>
    public bool Execute();
    /// <summary>
    /// Unexecutes the command, reverting any changes made by the Execute method.
    /// </summary>
    public void Unexecute();
    /// <summary>
    /// Gets the feature this command belongs to, or <c>null</c> if it is not subject to
    /// host-level feature gating (RadzenSpreadsheet's <c>ReadOnly</c> and <c>Allow*</c>
    /// parameters).
    /// </summary>
    public SpreadsheetFeature? Feature => null;
}

/// <summary>
/// Represents a stack for managing undo and redo operations in a spreadsheet.
/// </summary>
public class UndoRedoStack
{
    private readonly Stack<ICommand> undoStack = new();
    private readonly Stack<ICommand> redoStack = new();
    private readonly Worksheet worksheet;

    /// <summary>
    /// Initializes a new instance of the <see cref="UndoRedoStack"/> class bound to the
    /// worksheet whose commands it batches.
    /// </summary>
    public UndoRedoStack(Worksheet worksheet)
    {
        this.worksheet = worksheet ?? throw new ArgumentNullException(nameof(worksheet));
    }

    /// <summary>
    /// Event raised when the undo or redo stacks change.
    /// </summary>
    public event Action? Changed;

    /// <summary>
    /// Gets a value indicating whether the undo stack is empty.
    /// </summary>
    public bool CanUndo => undoStack.Count > 0;

    /// <summary>
    /// Gets a value indicating whether the redo stack is empty.
    /// </summary>
    public bool CanRedo => redoStack.Count > 0;

    /// <summary>
    /// Executes a command and adds it to the undo stack if successful.
    /// </summary>
    public bool Execute(ICommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        var result = worksheet.Batch(command.Execute);

        if (result)
        {
            undoStack.Push(command);
            redoStack.Clear();
        }

        Changed?.Invoke();
        return result;
    }

    /// <summary>
    /// Undoes the last executed command, moving it to the redo stack.
    /// </summary>

    public void Undo()
    {
        if (undoStack.Count > 0)
        {
            var command = undoStack.Pop();
            worksheet.Batch(command.Unexecute);
            redoStack.Push(command);
            Changed?.Invoke();
        }
    }

    /// <summary>
    /// Redoes the last undone command, moving it back to the undo stack.
    /// </summary>
    public void Redo()
    {
        if (redoStack.Count > 0)
        {
            var command = redoStack.Pop();
            var result = worksheet.Batch(command.Execute);

            if (result)
            {
                undoStack.Push(command);
            }

            Changed?.Invoke();
        }
    }
}