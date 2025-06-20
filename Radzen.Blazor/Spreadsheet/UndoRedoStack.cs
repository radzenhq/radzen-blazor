using System.Collections.Generic;

namespace Radzen.Blazor.Spreadsheet;

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
}

/// <summary>
/// Represents a stack for managing undo and redo operations in a spreadsheet.
/// </summary>
public class UndoRedoStack
{
    private readonly Stack<ICommand> undoStack = new();
    private readonly Stack<ICommand> redoStack = new();

    /// <summary>
    /// Executes a command and adds it to the undo stack if successful.
    /// </summary>
    public bool Execute(ICommand command)
    {
        var result = command.Execute();

        if (result)
        {
            undoStack.Push(command);
            redoStack.Clear();
        }

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
            command.Unexecute();
            redoStack.Push(command);
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
            Execute(command);
        }
    }
}