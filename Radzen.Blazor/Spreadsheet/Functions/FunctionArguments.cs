using System.Collections.Generic;

#nullable enable

namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Represents the arguments passed to a formula function, organized by parameter name.
/// </summary>
public class FunctionArguments(Cell currentCell)
{
    private readonly Dictionary<string, object> arguments = [];

    /// <summary>
    /// The cell currently being evaluated (context for functions like ROW/COLUMN when reference is omitted).
    /// </summary>
    public Cell CurrentCell => currentCell;

    /// <summary>
    /// Sets an argument value for the specified parameter name.
    /// </summary>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <param name="value">The argument value.</param>
    public void Set(string parameterName, object value)
    {
        arguments[parameterName] = value;
    }

    private object? Get(string parameterName)
    {
        return arguments.TryGetValue(parameterName, out var value) ? value : null;
    }

    /// <summary>
    /// Gets a single CellData value for the specified parameter name.
    /// </summary>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <returns>The CellData value, or null if not found.</returns>
    public CellData? GetSingle(string parameterName)
    {
        var value = Get(parameterName);
        return value as CellData;
    }

    /// <summary>
    /// Gets a range of CellData values for the specified parameter name.
    /// </summary>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <returns>The list of CellData values, or null if not found.</returns>
    public List<CellData>? GetRange(string parameterName)
    {
        var value = Get(parameterName);
        return value as List<CellData>;
    }

    /// <summary>
    /// Gets a sequence of CellData values for the specified parameter name.
    /// </summary>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <returns>The list of CellData values, or null if not found.</returns>
    public List<CellData>? GetSequence(string parameterName)
    {
        var value = Get(parameterName);
        return value as List<CellData>;
    }
}