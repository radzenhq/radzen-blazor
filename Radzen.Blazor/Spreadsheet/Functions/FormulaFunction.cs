using System.Collections.Generic;

#nullable enable
namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Base class for formula functions that provides common functionality for evaluation.
/// </summary>
public abstract class FormulaFunction
{
    /// <summary>
    /// Gets a value indicating whether this function can handle error arguments.
    /// Functions that return true will receive error values as arguments instead of having evaluation short-circuited.
    /// </summary>
    public virtual bool CanHandleErrors => false;

    /// <summary>
    /// Evaluates the function with the given arguments.
    /// </summary>
    /// <param name="arguments">The function arguments as CellData values.</param>
    /// <returns>The result value wrapped in CellData.</returns>
    public abstract CellData Evaluate(List<CellData> arguments);
}