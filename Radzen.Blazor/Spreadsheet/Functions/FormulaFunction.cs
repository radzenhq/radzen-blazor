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
    /// Gets the parameter definitions for this function.
    /// </summary>
    /// <returns>An array of parameter definitions describing the function's parameters.</returns>
    public abstract FunctionParameter[] Parameters { get; }

    /// <summary>
    /// Evaluates the function with the given arguments.
    /// </summary>
    /// <param name="arguments">The function arguments organized by parameter name.</param>
    /// <returns>The result value wrapped in CellData.</returns>
    public abstract CellData Evaluate(FunctionArguments arguments);
}